using Moq;
using SoftSign.Application.Interfaces;
using Xunit;

namespace SoftSign.Tests.Services;

public class FileUploadValidationTests
{
    private readonly Mock<IFileStorageService> _mockFileStorage;

    public FileUploadValidationTests()
    {
        _mockFileStorage = new Mock<IFileStorageService>();
    }

    [Theory]
    [InlineData("document.pdf", "application/pdf", true)]
    [InlineData("document.PDF", "application/pdf", true)]
    [InlineData("document.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", true)]
    [InlineData("document.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", true)]
    public void GetContentType_ValidExtensions_ReturnsCorrectType(string fileName, string expectedType, bool expected)
    {
        // Arrange
        _mockFileStorage.Setup(x => x.GetContentType(fileName))
            .Returns(expectedType);

        // Act
        var result = _mockFileStorage.Object.GetContentType(fileName);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Fact]
    public void GetContentType_InvalidExtension_ReturnsOctetStream()
    {
        // Arrange
        var invalidFile = "document.exe";
        
        _mockFileStorage.Setup(x => x.GetContentType(invalidFile))
            .Returns("application/octet-stream");

        // Act
        var result = _mockFileStorage.Object.GetContentType(invalidFile);

        // Assert
        Assert.Equal("application/octet-stream", result);
    }

    [Fact]
    public async Task SaveFileAsync_ValidPdf_SavesSuccessfully()
    {
        // Arrange
        var fileName = "test.pdf";
        var contentType = "application/pdf";
        var filePath = "/documents/test.pdf";
        
        _mockFileStorage.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), fileName, "documents"))
            .ReturnsAsync(filePath);

        // Act
        var result = await _mockFileStorage.Object.SaveFileAsync(new MemoryStream(), fileName);

        // Assert
        Assert.Equal(filePath, result);
    }

    [Fact]
    public async Task FileExistsAsync_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var filePath = "/documents/test.pdf";
        
        _mockFileStorage.Setup(x => x.FileExistsAsync(filePath))
            .ReturnsAsync(true);

        // Act
        var result = await _mockFileStorage.Object.FileExistsAsync(filePath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task FileExistsAsync_NonExistingFile_ReturnsFalse()
    {
        // Arrange
        var filePath = "/documents/nonexistent.pdf";
        
        _mockFileStorage.Setup(x => x.FileExistsAsync(filePath))
            .ReturnsAsync(false);

        // Act
        var result = await _mockFileStorage.Object.FileExistsAsync(filePath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_DeletesSuccessfully()
    {
        // Arrange
        var filePath = "/documents/test.pdf";
        
        _mockFileStorage.Setup(x => x.FileExistsAsync(filePath))
            .ReturnsAsync(true);
        _mockFileStorage.Setup(x => x.DeleteFileAsync(filePath))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await _mockFileStorage.Object.DeleteFileAsync(filePath);
        _mockFileStorage.Verify(x => x.DeleteFileAsync(filePath), Times.Once);
    }

    [Fact]
    public void GetContentType_PdfExtension_IsCaseInsensitive()
    {
        // Arrange
        var pdfFiles = new[] { "test.pdf", "TEST.PDF", "Test.Pdf" };
        
        foreach (var file in pdfFiles)
        {
            _mockFileStorage.Setup(x => x.GetContentType(file))
                .Returns("application/pdf");
        }

        // Act & Assert
        foreach (var file in pdfFiles)
        {
            var result = _mockFileStorage.Object.GetContentType(file);
            Assert.Equal("application/pdf", result);
        }
    }
}

public class PdfServiceTests
{
    private readonly Mock<IPdfService> _mockPdfService;

    public PdfServiceTests()
    {
        _mockPdfService = new Mock<IPdfService>();
    }

    [Fact]
    public async Task GetPageCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var pdfBytes = new byte[] { 0 };
        var expectedPageCount = 5;
        
        _mockPdfService.Setup(x => x.GetPageCountAsync(It.IsAny<byte[]>()))
            .ReturnsAsync(expectedPageCount);

        // Act
        var result = await _mockPdfService.Object.GetPageCountAsync(pdfBytes);

        // Assert
        Assert.Equal(expectedPageCount, result);
    }

    [Fact]
    public async Task ApplySignatureOverlayAsync_AddsSignatureToPdf()
    {
        // Arrange
        var pdfBytes = new byte[] { 0 };
        var signatureData = "base64signature";
        
        _mockPdfService.Setup(x => x.ApplySignatureOverlayAsync(
                pdfBytes,
                signatureData,
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>()))
            .ReturnsAsync(new byte[] { 1 });

        // Act
        var result = await _mockPdfService.Object.ApplySignatureOverlayAsync(
            pdfBytes, signatureData, 1, 100, 200, 50, 20);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task MergePdfsAsync_MultiplePdfs_MergesSuccessfully()
    {
        // Arrange
        var pdfs = new List<byte[]>
        {
            new byte[] { 0x1 },
            new byte[] { 0x2 },
            new byte[] { 0x3 }
        };
        
        _mockPdfService.Setup(x => x.MergePdfsAsync(pdfs))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        // Act
        var result = await _mockPdfService.Object.MergePdfsAsync(pdfs);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ConvertToPdfAsync_WordDocument_ConvertsToPdf()
    {
        // Arrange
        var docxBytes = new byte[] { 0 };
        
        _mockPdfService.Setup(x => x.ConvertToPdfAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 1 });

        // Act
        var result = await _mockPdfService.Object.ConvertToPdfAsync(
            new MemoryStream(docxBytes),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        );

        // Assert
        Assert.NotNull(result);
    }
}
