using SoftSign.Domain.Common;
using System;

namespace SoftSign.Domain.Entities;

public class DocumentWorkflowStep : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid WorkflowStepId { get; set; }
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? OriginalAssignedUserId { get; set; }
    public Guid? CustomAssignedUserId { get; set; }
    public Guid? OriginalAssignedRoleId { get; set; }
    public Guid? CustomAssignedRoleId { get; set; }
    public int StepType { get; set; }
    public string? Instructions { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Comments { get; set; }

    // Navigation properties
    public virtual Document Document { get; set; } = null!;
    public virtual WorkflowStep? OriginalWorkflowStep { get; set; }
    public virtual User? CustomAssignedUser { get; set; }
}
