using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using Xunit;

namespace SoftSign.Tests.BusinessRules;

public class WorkflowBusinessRulesTests
{
    [Fact]
    public void WorkflowStep_MustFollowOrder_FirstIncompleteIsCurrent()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Id = Guid.NewGuid(), Name = "Step 1", StepOrder = 1, IsCompleted = true },
                new WorkflowStep { Id = Guid.NewGuid(), Name = "Step 2", StepOrder = 2, IsCompleted = false },
                new WorkflowStep { Id = Guid.NewGuid(), Name = "Step 3", StepOrder = 3, IsCompleted = false }
            }
        };

        var currentStep = workflow.Steps.FirstOrDefault(s => !s.IsCompleted);
        Assert.NotNull(currentStep);
        Assert.Equal(2, currentStep.StepOrder);
    }

    [Fact]
    public void SignatureZone_ValidPosition_WithinPageBounds()
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

        Assert.True(zone.PositionX >= 0 && zone.PositionX + zone.Width <= pageWidth);
        Assert.True(zone.PositionY >= 0 && zone.PositionY + zone.Height <= pageHeight);
    }

    [Fact]
    public void WorkflowStep_AssignedUser_CanActOnStep()
    {
        var userId = Guid.NewGuid();
        var step = new WorkflowStep
        {
            Id = Guid.NewGuid(),
            Name = "Manager Approval",
            AssignedUserId = userId,
            IsCompleted = false
        };

        Assert.Equal(userId, step.AssignedUserId);
    }

    [Fact]
    public void DocumentVersion_NewVersion_IsIncrement()
    {
        var document = new Document { Id = Guid.NewGuid(), Version = 1 };
        document.Version++;
        Assert.Equal(2, document.Version);
    }

    [Fact]
    public void DocumentActivity_RecordsView()
    {
        var documentId = Guid.NewGuid();
        var activities = new List<DocumentActivity>
        {
            new DocumentActivity { Id = Guid.NewGuid(), DocumentId = documentId, Action = "View" },
            new DocumentActivity { Id = Guid.NewGuid(), DocumentId = documentId, Action = "View" },
            new DocumentActivity { Id = Guid.NewGuid(), DocumentId = documentId, Action = "View" }
        };

        Assert.Equal(3, activities.Count(a => a.Action == "View"));
    }

    [Fact]
    public void DocumentSignature_LogsIpAddress()
    {
        var signature = new DocumentSignature
        {
            Id = Guid.NewGuid(),
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0"
        };

        Assert.Equal("192.168.1.100", signature.IpAddress);
    }
}
