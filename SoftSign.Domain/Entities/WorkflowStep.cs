using SoftSign.Domain.Common;
using SoftSign.Domain.Enums;

namespace SoftSign.Domain.Entities;

public class WorkflowStep : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public StepType StepType { get; set; } = StepType.Signature;
    public string? Instructions { get; set; }
    public SignatureLevel RequiredSignatureLevel { get; set; } = SignatureLevel.Signature;
    public Guid? AssignedUserId { get; set; }
    public Guid? AssignedRoleId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Comments { get; set; }
    
    public int RequiredSignatureCount { get; set; } = 1;

    public virtual Workflow Workflow { get; set; } = null!;
    public virtual User? AssignedUser { get; set; }
}
