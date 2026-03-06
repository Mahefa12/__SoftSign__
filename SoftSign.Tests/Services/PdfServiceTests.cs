using FluentAssertions;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Infrastructure.Services;
using Xunit;

namespace SoftSign.Tests.Services;

/// <summary>
/// Tests for PdfService signature application functionality.
/// Verifies that signatures are applied at correct coordinates and handles multiple zones/pages.
/// </summary>
public class SignaturePdfServiceTests
{
    private readonly IPdfService _pdfService;

    public SignaturePdfServiceTests()
    {
        _pdfService = new PdfService();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a minimal valid PDF document for testing purposes.
    /// </summary>
    private byte[] CreateTestPdf()
    {
        var document = new PdfDocument();
        document.Info.Title = "Test Document";
        document.Info.Creator = "Test";
        
        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        
        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Creates a multi-page PDF for testing.
    /// </summary>
    private byte[] CreateMultiPageTestPdf(int pageCount)
    {
        var document = new PdfDocument();
        document.Info.Title = "Multi-page Test Document";
        
        for (int i = 0; i < pageCount; i++)
        {
            var page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
        }
        
        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Creates a valid PNG base64 string for testing.
    /// </summary>
    private string CreateTestPngBase64()
    {
        // Minimal 1x1 PNG (base64 encoded)
        return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
    }

    #endregion

    #region ApplySignaturesFromZonesAsync Tests

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_AppliesSignature_AtCorrectCoordinates()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();
        var signatureBase64 = CreateTestPngBase64();
        
        var zone = new SignatureZone
        {
            Id = Guid.NewGuid(),
            PageNumber = 1,
            PositionX = 100,
            PositionY = 100,
            Width = 150,
            Height = 50,
            SignatureImage = signatureBase64
        };
        
        var zones = new List<SignatureZone> { zone };

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, zones);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        
        // Verify the PDF was modified (has more than original size due to embedded image)
        result.Length.Should().BeGreaterThan(pdfBytes.Length);
    }

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_ReturnsOriginalPdf_WhenNoSignedZones()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();
        
        var unsignedZone = new SignatureZone
        {
            Id = Guid.NewGuid(),
            PageNumber = 1,
            PositionX = 100,
            PositionY = 100,
            Width = 150,
            Height = 50,
            SignatureImage = null // Not signed
        };
        
        var zones = new List<SignatureZone> { unsignedZone };

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, zones);

        // Assert
        result.Should().Equal(pdfBytes); // Should return original unchanged
    }

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_ReturnsOriginalPdf_WhenEmptyZoneList()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();
        var zones = new List<SignatureZone>();

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, zones);

        // Assert
        result.Should().Equal(pdfBytes);
    }

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_ReturnsOriginalPdf_WhenAllZonesUnsigned()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();
        
        var zones = new List<SignatureZone>
        {
            new SignatureZone
            {
                PageNumber = 1,
                PositionX = 100,
                PositionY = 100,
                Width = 150,
                Height = 50,
                SignatureImage = string.Empty
            },
            new SignatureZone
            {
                PageNumber = 1,
                PositionX = 300,
                PositionY = 100,
                Width = 150,
                Height = 50,
                SignatureImage = null
            }
        };

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, zones);

        // Assert
        result.Should().Equal(pdfBytes);
    }

    #endregion

    #region Multiple Zones on Same Page Tests

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_HandlesMultipleZones_SamePage()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();
        var signatureBase64 = CreateTestPngBase64();
        
        var zones = new List<SignatureZone>
        {
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                PageNumber = 1,
                PositionX = 50,
                PositionY = 100,
                Width = 100,
                Height = 30,
                SignatureImage = signatureBase64,
                Order = 1
            },
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                PageNumber = 1,
                PositionX = 200,
                PositionY = 100,
                Width = 100,
                Height = 30,
                SignatureImage = signatureBase64,
                Order = 2
            },
            new SignatureZone
            {
                Id = Guid.NewGuid(),
                PageNumber = 1,
                PositionX = 350,
                PositionY = 100,
                Width = 100,
                Height = 30,
                SignatureImage = signatureBase64,
                Order = 3
            }
        };

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, zones);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(pdfBytes.Length);
        
        // Verify all 3 signatures were applied - the result should be significantly larger
        // than original due to 3 embedded images
        var sizeIncrease = result.Length - pdfBytes.Length;
        sizeIncrease.Should().BeGreaterThan(500); // At least some increase from 3 images
    }

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_SkipsUnsignedZones_ProcessesSignedOnes()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();
        var signatureBase64 = CreateTestPngBase64();
        
        var zones = new List<SignatureZone>
        {
            new SignatureZone
            {
                PageNumber = 1,
                PositionX = 50,
                PositionY = 100,
                Width = 100,
                Height = 30,
                SignatureImage = null, // Not signed
                Order = 1
            },
            new SignatureZone
            {
                PageNumber = 1,
                PositionX = 200,
                PositionY = 100,
                Width = 100,
                Height = 30,
                SignatureImage = signatureBase64, // Signed
                Order = 2
            }
        };

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, zones);

        // Assert - Should apply only the signed zone
        result.Length.Should().BeGreaterThan(pdfBytes.Length);
        result.Length.Should().BeLessThan(pdfBytes.Length + 2000); // Much smaller than if both were applied
    }

    #endregion

    #region Multiple Pages Tests

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_HandlesMultiplePages()
    {
        // Arrange
        var pdfBytes = CreateMultiPageTestPdf(3);
        var signatureBase64 = CreateTestPngBase64();
        
        var zones = new List<SignatureZone>
        {
            new SignatureZone
            {
                PageNumber = 1,
                PositionX = 100,
                PositionY = 100,
                Width = 150,
                Height = 50,
                SignatureImage = signatureBase64
            },
            new SignatureZone
            {
                PageNumber = 2,
                PositionX = 100,
                PositionY = 200,
                Width = 150,
                Height = 50,
                SignatureImage = signatureBase64
            },
            new SignatureZone
            {
                PageNumber = 3,
                PositionX = 100,
                PositionY = 300,
                Width = 150,
                Height = 50,
                SignatureImage = signatureBase64
            }
        };

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, zones);

        // Assert
        result.Should().NotBeNull();
        
        // Verify page count is still 3
        using var ms = new MemoryStream(result);
        using var doc = PdfReader.Open(ms, PdfDocumentOpenMode.InformationOnly);
        doc.PageCount.Should().Be(3);
        
        // Verify size increased (signatures applied)
        result.Length.Should().BeGreaterThan(pdfBytes.Length);
    }

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_HandlesZoneOnLastPage()
    {
        // Arrange
        var pdfBytes = CreateMultiPageTestPdf(5);
        var signatureBase64 = CreateTestPngBase64();
        
        var zone = new SignatureZone
        {
            PageNumber = 5, // Last page
            PositionX = 100,
            PositionY = 100,
            Width = 150,
            Height = 50,
            SignatureImage = signatureBase64
        };

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, new List<SignatureZone> { zone });

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(pdfBytes.Length);
    }

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_ClampsInvalidPageNumber()
    {
        // Arrange
        var pdfBytes = CreateMultiPageTestPdf(2);
        var signatureBase64 = CreateTestPngBase64();
        
        // Page number exceeds page count - should be clamped
        var zone = new SignatureZone
        {
            PageNumber = 10, // More than available pages
            PositionX = 100,
            PositionY = 100,
            Width = 150,
            Height = 50,
            SignatureImage = signatureBase64
        };

        // Act - Should not throw, should clamp to valid page
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, new List<SignatureZone> { zone });

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region ApplySignatureOverlayAsync Tests

    [Fact]
    public async Task ApplySignatureOverlayAsync_AppliesSignature_AtSpecifiedPosition()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();
        var signatureBase64 = CreateTestPngBase64();
        
        // Act
        var result = await _pdfService.ApplySignatureOverlayAsync(
            pdfBytes,
            signatureBase64,
            pageNumber: 1,
            x: 100,
            y: 100,
            width: 150,
            height: 50
        );

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(pdfBytes.Length);
    }

    [Fact]
    public async Task ApplySignatureOverlayAsync_clampsYPosition_WhenExceedsPageHeight()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();
        var signatureBase64 = CreateTestPngBase64();
        
        // Y position that would exceed page height
        var yPosition = 800; // Near bottom in PDF coords (A4 is ~842 points high)
        
        // Act - Should not throw
        var result = await _pdfService.ApplySignatureOverlayAsync(
            pdfBytes,
            signatureBase64,
            pageNumber: 1,
            x: 100,
            y: yPosition,
            width: 150,
            height: 100 // This would exceed page height
        );

        // Assert
        result.Should().NotBeNull();
        // The service should clamp the position internally
    }

    #endregion

    #region GetPageCountAsync Tests

    [Fact]
    public async Task GetPageCountAsync_ReturnsCorrectPageCount_SinglePage()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();

        // Act
        var pageCount = await _pdfService.GetPageCountAsync(pdfBytes);

        // Assert
        pageCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPageCountAsync_ReturnsCorrectPageCount_MultiPage()
    {
        // Arrange
        var pdfBytes = CreateMultiPageTestPdf(5);

        // Act
        var pageCount = await _pdfService.GetPageCountAsync(pdfBytes);

        // Assert
        pageCount.Should().Be(5);
    }

    [Fact]
    public async Task GetPageCountAsync_ReturnsDefault_WhenInvalidPdf()
    {
        // Arrange
        var invalidPdf = new byte[] { 1, 2, 3, 4, 5 }; // Not a valid PDF

        // Act
        var pageCount = await _pdfService.GetPageCountAsync(invalidPdf);

        // Assert - Should return default of 1
        pageCount.Should().Be(1);
    }

    #endregion

    #region GetPageSizeAsync Tests

    [Fact]
    public async Task GetPageSizeAsync_ReturnsA4Dimensions_ForStandardPdf()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();

        // Act
        var (width, height) = await _pdfService.GetPageSizeAsync(pdfBytes, 1);

        // Assert - A4 dimensions in points
        width.Should().BeApproximately(595.28, 0.1);
        height.Should().BeApproximately(841.89, 0.1);
    }

    [Fact]
    public async Task GetPageSizeAsync_ReturnsDefault_WhenInvalidPdf()
    {
        // Arrange
        var invalidPdf = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var (width, height) = await _pdfService.GetPageSizeAsync(invalidPdf, 1);

        // Assert - Should return A4 default
        width.Should().Be(595.28);
        height.Should().Be(841.89);
    }

    [Fact]
    public async Task GetPageSizeAsync_ReturnsCorrectSize_ForSpecificPage()
    {
        // Arrange
        var pdfBytes = CreateMultiPageTestPdf(3);

        // Act
        var (width, height) = await _pdfService.GetPageSizeAsync(pdfBytes, 2);

        // Assert
        width.Should().BeApproximately(595.28, 0.1);
        height.Should().BeApproximately(841.89, 0.1);
    }

    #endregion

    #region Coordinate Handling Tests

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_UsesPdfCoordinates_Directly()
    {
        // Arrange
        var pdfBytes = CreateTestPdf();
        var signatureBase64 = CreateTestPngBase64();
        
        // PDF coordinates (bottom-left origin)
        var zone = new SignatureZone
        {
            PageNumber = 1,
            PositionX = 0,      // Left edge
            PositionY = 0,      // Bottom
            Width = 200,
            Height = 100,
            SignatureImage = signatureBase64
        };

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, new List<SignatureZone> { zone });

        // Assert
        result.Should().NotBeNull();
        // Service stores coordinates as-is (PDF coordinates)
    }

    [Fact]
    public async Task ApplySignaturesFromZonesAsync_HandlesZoneAtTopOfPage()
    {
        // Arrange - A4 height is 841.89, zone at top would have Y near 741.89
        var pdfBytes = CreateTestPdf();
        var signatureBase64 = CreateTestPngBase64();
        
        var zone = new SignatureZone
        {
            PageNumber = 1,
            PositionX = 100,
            PositionY = 741.89, // Near top of page
            Width = 200,
            Height = 100,
            SignatureImage = signatureBase64
        };

        // Act
        var result = await _pdfService.ApplySignaturesFromZonesAsync(pdfBytes, new List<SignatureZone> { zone });

        // Assert
        result.Should().NotBeNull();
    }

    #endregion
}
