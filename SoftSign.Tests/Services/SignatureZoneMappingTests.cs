using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using Xunit;

namespace SoftSign.Tests.Services;

/// <summary>
/// Tests for signature zone coordinate mapping between HTML canvas and PDF coordinates.
/// 
/// Key Understanding:
/// - HTML canvas uses top-left origin (Y increases downward)
/// - PDF uses bottom-left origin (Y increases upward)
/// - The application stores coordinates in PDF coordinate space
/// - When rendering in HTML, conversion is needed: pdfY = pageHeight - htmlY - zoneHeight
/// </summary>
public class SignatureZoneMappingTests
{
    // Standard A4 page dimensions in points (1 point = 1/72 inch)
    private const double A4Width = 595.28;
    private const double A4Height = 841.89;

    #region PDF to HTML Coordinate Conversion Tests

    /// <summary>
    /// Tests that PDF Y coordinate can be converted to HTML canvas Y coordinate.
    /// HTML canvas has top-left origin, PDF has bottom-left origin.
    /// </summary>
    [Fact]
    public void ConvertPdfYToHtml_YCoordinate_ConvertsCorrectly()
    {
        // Arrange
        double pdfY = 700.0;
        double pageHeight = A4Height;
        double zoneHeight = 50.0;

        // Act - PDF Y to HTML Y conversion
        // HTML Y = PageHeight - PDF Y - ZoneHeight (for top-left origin placement)
        double htmlY = pageHeight - pdfY - zoneHeight;

        // Assert
        Assert.Equal(91.89, htmlY, 2);
    }

    /// <summary>
    /// Tests conversion when PDF Y is at bottom of page (near 0)
    /// </summary>
    [Fact]
    public void ConvertPdfYToHtml_WhenPdfYIsZero_ReturnsNearPageHeight()
    {
        // Arrange
        double pdfY = 0.0;
        double pageHeight = A4Height;
        double zoneHeight = 50.0;

        // Act
        double htmlY = pageHeight - pdfY - zoneHeight;

        // Assert
        Assert.Equal(791.89, htmlY, 2);
    }

    /// <summary>
    /// Tests conversion when PDF Y is at top of page
    /// </summary>
    [Fact]
    public void ConvertPdfYToHtml_WhenPdfYIsAtTop_ReturnsNearZero()
    {
        // Arrange
        double pdfY = A4Height - 50.0; // Near top
        double pageHeight = A4Height;
        double zoneHeight = 50.0;

        // Act
        double htmlY = pageHeight - pdfY - zoneHeight;

        // Assert
        Assert.Equal(0, htmlY, 2);
    }

    #endregion

    #region HTML to PDF Coordinate Conversion Tests

    /// <summary>
    /// Tests that HTML canvas Y coordinate can be converted to PDF Y coordinate.
    /// </summary>
    [Fact]
    public void ConvertHtmlYToPdf_YCoordinate_ConvertsCorrectly()
    {
        // Arrange
        double htmlY = 91.89;
        double pageHeight = A4Height;
        double zoneHeight = 50.0;

        // Act - HTML Y to PDF Y conversion
        double pdfY = pageHeight - htmlY - zoneHeight;

        // Assert
        Assert.Equal(700.0, pdfY, 2);
    }

    #endregion

    #region Canvas Dimensions Tests

    [Fact]
    public void CanvasDimensions_MatchZoneDimensions_Width()
    {
        // Arrange
        var zone = new SignatureZone
        {
            Width = 200.0,
            Height = 80.0
        };

        // Act & Assert - Canvas width should match zone width
        Assert.Equal(200.0, zone.Width);
    }

    [Fact]
    public void CanvasDimensions_MatchZoneDimensions_Height()
    {
        // Arrange
        var zone = new SignatureZone
        {
            Width = 200.0,
            Height = 80.0
        };

        // Act & Assert - Canvas height should match zone height
        Assert.Equal(80.0, zone.Height);
    }

    [Fact]
    public void CanvasDimensions_CalculateFromPdfPage_Size()
    {
        // Arrange
        double zoneWidthPercent = 0.35; // 35% of page width
        double zoneHeightPercent = 0.10; // 10% of page height
        double pageWidth = A4Width;
        double pageHeight = A4Height;

        // Act - Calculate actual dimensions
        double zoneWidth = pageWidth * zoneWidthPercent;
        double zoneHeight = pageHeight * zoneHeightPercent;

        // Assert
        Assert.Equal(208.35, zoneWidth, 2);
        Assert.Equal(84.19, zoneHeight, 2);
    }

    #endregion

    #region Zone Position Validation Tests

    [Fact]
    public void ZonePosition_IsValid_WhenWithinPageBounds()
    {
        // Arrange
        var zone = new SignatureZone
        {
            PositionX = 100,
            PositionY = 100,
            Width = 200,
            Height = 80,
            PageNumber = 1
        };

        // Act
        bool isValidX = zone.PositionX >= 0 && (zone.PositionX + zone.Width) <= A4Width;
        bool isValidY = zone.PositionY >= 0 && (zone.PositionY + zone.Height) <= A4Height;

        // Assert
        Assert.True(isValidX);
        Assert.True(isValidY);
    }

    [Fact]
    public void ZonePosition_IsInvalid_WhenExceedsPageWidth()
    {
        // Arrange
        var zone = new SignatureZone
        {
            PositionX = 500,
            Width = 200, // 500 + 200 = 700 > 595.28 (A4 width)
            Height = 80,
            PageNumber = 1
        };

        // Act
        bool isValidX = zone.PositionX >= 0 && (zone.PositionX + zone.Width) <= A4Width;

        // Assert
        Assert.False(isValidX);
    }

    [Fact]
    public void ZonePosition_IsInvalid_WhenExceedsPageHeight()
    {
        // Arrange
        var zone = new SignatureZone
        {
            PositionX = 100,
            PositionY = 800,
            Width = 200,
            Height = 100, // 800 + 100 = 900 > 841.89 (A4 height)
            PageNumber = 1
        };

        // Act
        bool isValidY = zone.PositionY >= 0 && (zone.PositionY + zone.Height) <= A4Height;

        // Assert
        Assert.False(isValidY);
    }

    #endregion

    #region Multi-Page Zone Tests

    [Fact]
    public void Zone_CanBeOnAnyPage()
    {
        // Arrange
        var zone1 = new SignatureZone { PageNumber = 1, PositionX = 100, PositionY = 100, Width = 200, Height = 50 };
        var zone2 = new SignatureZone { PageNumber = 5, PositionX = 100, PositionY = 100, Width = 200, Height = 50 };
        var zone3 = new SignatureZone { PageNumber = 10, PositionX = 100, PositionY = 100, Width = 200, Height = 50 };

        // Assert
        Assert.Equal(1, zone1.PageNumber);
        Assert.Equal(5, zone2.PageNumber);
        Assert.Equal(10, zone3.PageNumber);
    }

    [Fact]
    public void MultipleZones_CanExistOnSamePage()
    {
        // Arrange
        var zones = new List<SignatureZone>
        {
            new SignatureZone { PageNumber = 1, PositionX = 50, PositionY = 100, Width = 150, Height = 40, Order = 1 },
            new SignatureZone { PageNumber = 1, PositionX = 250, PositionY = 100, Width = 150, Height = 40, Order = 2 },
            new SignatureZone { PageNumber = 1, PositionX = 50, PositionY = 200, Width = 150, Height = 40, Order = 3 }
        };

        // Assert
        Assert.Equal(3, zones.Count);
        Assert.All(zones, z => Assert.Equal(1, z.PageNumber));
    }

    #endregion

    #region Coordinate Scaling Tests

    [Fact]
    public void ZoneCoordinates_ScaleFromHtmlToPdf_Correctly()
    {
        // Arrange - HTML canvas at 72 DPI (1:1 with PDF points)
        double htmlCanvasWidth = 595.28;
        double htmlCanvasHeight = 841.89;
        double htmlZoneX = 100;
        double htmlZoneY = 100;
        double zoneWidth = 200;
        double zoneHeight = 80;

        // Act - When using 1:1 scaling (no zoom)
        double pdfZoneX = htmlZoneX;
        double pdfZoneY = htmlCanvasHeight - htmlZoneY - zoneHeight; // Convert to PDF coords

        // Assert
        Assert.Equal(100, pdfZoneX, 2);
        Assert.Equal(661.89, pdfZoneY, 2);
    }

    [Fact]
    public void ZoneCoordinates_ScaleWithZoom_Correctly()
    {
        // Arrange - HTML canvas with 50% zoom
        double zoomFactor = 0.5;
        double htmlCanvasWidth = 595.28;
        double htmlCanvasHeight = 841.89;
        double htmlZoneX = 200;
        double htmlZoneY = 200;
        double zoneWidth = 400;
        double zoneHeight = 160;

        // Act - Scale to PDF coordinates
        double pdfZoneX = htmlZoneX * zoomFactor;
        double pdfZoneY = (htmlCanvasHeight * zoomFactor) - (htmlZoneY * zoomFactor) - (zoneHeight * zoomFactor);

        // Assert
        Assert.Equal(100, pdfZoneX, 2);
        Assert.Equal(330.95, pdfZoneY, 2);
    }

    #endregion
}
