using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;

namespace SoftSign.Tests.Performance;

public class DocumentPerformanceTests
{
    private Document _document = null!;
    private List<DocumentSignature> _signatures = null!;
    private List<DocumentActivity> _activities = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Performance Test Document",
            FileName = "large_document.pdf",
            OriginalFileName = "large_document.pdf",
            FilePath = "/documents/large_document.pdf",
            ContentType = "application/pdf",
            FileSize = 10 * 1024 * 1024,
            Version = 1,
            Status = DocumentStatus.Draft,
            CreatedById = Guid.NewGuid()
        };

        _signatures = Enumerable.Range(0, 100)
            .Select(i => new DocumentSignature
            {
                Id = Guid.NewGuid(),
                DocumentId = _document.Id,
                SignerId = Guid.NewGuid(),
                Level = SignatureLevel.Signature,
                IsSigned = i % 2 == 0,
                SignedAt = i % 2 == 0 ? DateTime.UtcNow : null
            })
            .ToList();

        _activities = Enumerable.Range(0, 1000)
            .Select(i => new DocumentActivity
            {
                Id = Guid.NewGuid(),
                DocumentId = _document.Id,
                UserId = Guid.NewGuid(),
                Action = i % 3 == 0 ? "View" : (i % 3 == 1 ? "Sign" : "Comment"),
                Details = $"Activity detail {i}",
                IpAddress = "192.168.1." + (i % 255)
            })
            .ToList();
    }

    [Benchmark]
    public int Document_SignatureCount_Calculation()
    {
        return _signatures.Count(s => s.IsSigned);
    }

    [Benchmark]
    public int Document_ActivityCount_ByAction()
    {
        return _activities.Count(a => a.Action == "View");
    }

    [Benchmark]
    public bool Document_StatusTransition_Validation()
    {
        var currentStatus = DocumentStatus.Draft;
        var newStatus = DocumentStatus.Pending;
        
        return (currentStatus, newStatus) switch
        {
            (DocumentStatus.Draft, DocumentStatus.Pending) => true,
            (DocumentStatus.Pending, DocumentStatus.InProgress) => true,
            (DocumentStatus.InProgress, DocumentStatus.Completed) => true,
            (DocumentStatus.InProgress, DocumentStatus.Rejected) => true,
            (DocumentStatus.Pending, DocumentStatus.Cancelled) => true,
            _ => false
        };
    }

    [Benchmark]
    public Guid Document_FindNextWorkflowStep()
    {
        var steps = new List<WorkflowStep>
        {
            new() { Id = Guid.NewGuid(), Name = "Step 1", StepOrder = 1, IsCompleted = true },
            new() { Id = Guid.NewGuid(), Name = "Step 2", StepOrder = 2, IsCompleted = false },
            new() { Id = Guid.NewGuid(), Name = "Step 3", StepOrder = 3, IsCompleted = false }
        };

        return steps.FirstOrDefault(s => !s.IsCompleted)?.Id ?? Guid.Empty;
    }

    [Benchmark]
    public string Document_GenerateActivitySummary()
    {
        var viewCount = _activities.Count(a => a.Action == "View");
        var signCount = _activities.Count(a => a.Action == "Sign");
        var commentCount = _activities.Count(a => a.Action == "Comment");
        
        return $"Document has {viewCount} views, {signCount} signatures, and {commentCount} comments.";
    }

    [Benchmark]
    public bool Document_ValidateSignatureZone()
    {
        var zone = new SignatureZone
        {
            PageNumber = 1,
            PositionX = 100,
            PositionY = 200,
            Width = 50,
            Height = 20
        };

        const double pageWidth = 595;
        const double pageHeight = 842;

        return zone.PositionX >= 0 && 
               zone.PositionX + zone.Width <= pageWidth &&
               zone.PositionY >= 0 && 
               zone.PositionY + zone.Height <= pageHeight;
    }
}
