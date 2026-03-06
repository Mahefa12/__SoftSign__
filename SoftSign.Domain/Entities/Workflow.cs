using SoftSign.Domain.Common;
using SoftSign.Domain.Enums;

namespace SoftSign.Domain.Entities;

public class Workflow : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Active;
    public Guid? CompanyId { get; set; }
    public int Order { get; set; }
    public bool IsDefault { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Company? Company { get; set; }
    public virtual ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
