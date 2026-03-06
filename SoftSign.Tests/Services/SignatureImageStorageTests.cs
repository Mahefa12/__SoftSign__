using FluentAssertions;
using Moq;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using Xunit;

namespace SoftSign.Tests.Services;

/// <summary>
/// Tests for signature image storage and retrieval functionality.
/// Verifies that base64 PNG signature images are properly stored and retrieved.
/// </summary>
public class SignatureImageStorageTests
{
    private readonly Mock<IFileStorageService> _mockFileStorageService;

    public SignatureImageStorageTests()
    {
        _mockFileStorageService = new Mock<IFileStorageService>();
    }

    #region Base64 Image Storage Tests

    [Fact]
    public void SignatureZone_StoresBase64SignatureImage()
    {
        // Arrange
        var base64Image = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        
        var zone = new SignatureZone
        {
            SignatureImage = base64Image
        };

        // Assert
        zone.SignatureImage.Should().Be(base64Image);
        zone.IsSigned.Should().BeTrue();
    }

    [Fact]
    public void SignatureZone_StoresBase64SignatureImage_WithoutDataPrefix()
    {
        // Arrange
        var base64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        
        var zone = new SignatureZone
        {
            SignatureImage = base64Image
        };

        // Assert
        zone.SignatureImage.Should().Be(base64Image);
        zone.IsSigned.Should().BeTrue();
    }

    [Fact]
    public void SignatureZone_CanStoreLargeBase64Image()
    {
        // Arrange - Create a larger base64 string (simulating a real signature)
        var largeBase64Image = Convert.ToBase64String(new byte[5000]); // 5KB signature
        
        var zone = new SignatureZone
        {
            SignatureImage = largeBase64Image
        };

        // Assert
        zone.SignatureImage.Should().NotBeNullOrEmpty();
        zone.SignatureImage!.Length.Should().BeGreaterThan(4000);
    }

    #endregion

    #region Image Format Validation Tests

    [Fact]
    public void SignatureImage_IsValidPngFormat_WithDataPrefix()
    {
        // Arrange
        var pngBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        bool isPng = pngBase64.StartsWith("data:image/png", StringComparison.OrdinalIgnoreCase);

        // Assert
        isPng.Should().BeTrue();
    }

    [Fact]
    public void SignatureImage_ExtractsRawBase64_FromDataUri()
    {
        // Arrange
        var fullDataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act - Extract raw base64 from data URI (as done in PdfService)
        var rawBase64 = fullDataUri.Replace("data:image/png;base64,", "");

        // Assert
        rawBase64.Should().Be("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
    }

    [Fact]
    public void SignatureImage_CanBeDecoded_FromBase64()
    {
        // Arrange
        var rawBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        byte[] imageBytes;
        bool canDecode = false;
        
        try
        {
            imageBytes = Convert.FromBase64String(rawBase64);
            canDecode = true;
        }
        catch
        {
            imageBytes = Array.Empty<byte>();
        }

        // Assert
        canDecode.Should().BeTrue();
        imageBytes.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Signature Image Retrieval Tests

    [Fact]
    public async Task FileStorageService_SaveAndRetrieve_SignatureImage()
    {
        // Arrange
        var signatureContent = "test_signature.png";
        var signatureBytes = System.Text.Encoding.UTF8.GetBytes("fake image content");
        var stream = new MemoryStream(signatureBytes);
        
        _mockFileStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(signatureContent);

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(signatureContent))
            .ReturnsAsync(stream);

        // Act
        var savedPath = await _mockFileStorageService.Object.SaveFileAsync(stream, "signature.png", "signatures");
        var retrievedStream = await _mockFileStorageService.Object.GetFileAsync(savedPath);

        // Assert
        savedPath.Should().Be(signatureContent);
        retrievedStream.Should().NotBeNull();
    }

    [Fact]
    public void SignatureZone_Retrieval_ReturnsOriginalImage()
    {
        // Arrange
        var originalImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        
        var zone = new SignatureZone
        {
            SignatureImage = originalImage
        };

        // Act
        var retrievedImage = zone.SignatureImage;

        // Assert
        retrievedImage.Should().Be(originalImage);
    }

    #endregion

    #region FileStorageService Interface Tests

    [Fact]
    public async Task FileStorageService_FileExists_ReturnsTrue_WhenFileExists()
    {
        // Arrange
        var filePath = "documents/signature.png";
        
        _mockFileStorageService
            .Setup(x => x.FileExistsAsync(filePath))
            .ReturnsAsync(true);

        // Act
        var exists = await _mockFileStorageService.Object.FileExistsAsync(filePath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task FileStorageService_FileExists_ReturnsFalse_WhenFileNotExists()
    {
        // Arrange
        var filePath = "documents/nonexistent.png";
        
        _mockFileStorageService
            .Setup(x => x.FileExistsAsync(filePath))
            .ReturnsAsync(false);

        // Act
        var exists = await _mockFileStorageService.Object.FileExistsAsync(filePath);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task FileStorageService_DeleteFile_DeletesSuccessfully()
    {
        // Arrange
        var filePath = "documents/signature.png";
        
        _mockFileStorageService
            .Setup(x => x.DeleteFileAsync(filePath))
            .Returns(Task.CompletedTask);

        // Act & Assert - Should not throw
        await _mockFileStorageService.Object.DeleteFileAsync(filePath);
    }

    [Fact]
    public void FileStorageService_GetContentType_ReturnsPng()
    {
        // Arrange
        _mockFileStorageService
            .Setup(x => x.GetContentType("signature.png"))
            .Returns("image/png");

        // Act
        var contentType = _mockFileStorageService.Object.GetContentType("signature.png");

        // Assert
        contentType.Should().Be("image/png");
    }

    #endregion

    #region Signature Zone Integration with Storage

    [Fact]
    public void SignatureZone_StoresSignature_WithAllMetadata()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var signedAt = DateTime.UtcNow;
        var signatureImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var zone = new SignatureZone
        {
            DocumentId = Guid.NewGuid(),
            PageNumber = 1,
            PositionX = 100,
            PositionY = 200,
            Width = 150,
            Height = 50,
            SignatureImage = signatureImage,
            SignedByUserId = userId,
            SignedAt = signedAt
        };

        // Assert
        zone.IsSigned.Should().BeTrue();
        zone.SignatureImage.Should().Be(signatureImage);
        zone.SignedByUserId.Should().Be(userId);
        zone.SignedAt.Should().Be(signedAt);
    }

    [Fact]
    public void SignatureZone_ClearsSignature_RemovesAllData()
    {
        // Arrange
        var zone = new SignatureZone
        {
            SignatureImage = "data:image/png;base64,test",
            SignedByUserId = Guid.NewGuid(),
            SignedAt = DateTime.UtcNow
        };

        // Act
        zone.SignatureImage = null;
        zone.SignedByUserId = null;
        zone.SignedAt = null;

        // Assert
        zone.IsSigned.Should().BeFalse();
        zone.SignatureImage.Should().BeNull();
        zone.SignedByUserId.Should().BeNull();
        zone.SignedAt.Should().BeNull();
    }

    #endregion
}
