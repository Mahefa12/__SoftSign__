using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using Xunit;

namespace SoftSign.Tests.Entities;

public class DocumentTests
{
    [Fact]
    public void Document_Constructor_SetsDefaultValues()
    {
        var document = new Document();
        Assert.Equal(string.Empty, document.Title);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(1, document.Version);
        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.False(document.IsDeleted);
    }

    [Fact]
    public void Document_Version_StartsAtOne()
    {
        var document = new Document();
        Assert.Equal(1, document.Version);
    }

    [Fact]
    public void Document_Status_CanBeSetToCompleted()
    {
        var document = new Document { Status = DocumentStatus.Completed };
        Assert.Equal(DocumentStatus.Completed, document.Status);
    }

    [Fact]
    public void Document_IsDeleted_DefaultsToFalse()
    {
        var document = new Document();
        Assert.False(document.IsDeleted);
    }
}
