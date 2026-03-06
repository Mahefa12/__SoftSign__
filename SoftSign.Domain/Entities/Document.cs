using SoftSign.Domain.Common;
using SoftSign.Domain.Enums;

namespace SoftSign.Domain.Entities;

public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? SignedFilePath { get; set; }
    public string ContentType { get; set; } = "application/pdf";
    public long FileSize { get; set; }
    public int Version { get; set; } = 1;
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public Guid? CompanyId { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? WorkflowId { get; set; }
    public bool IsWorkflowCustomized { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureHash { get; set; }

    public virtual Company? Company { get; set; }
    public virtual User CreatedBy { get; set; } = null!;
    public virtual Workflow? Workflow { get; set; }
    public virtual ICollection<DocumentSignature> Signatures { get; set; } = new List<DocumentSignature>();
    public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public virtual ICollection<SignatureZone> SignatureZones { get; set; } = new List<SignatureZone>();
    public virtual ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();
    public virtual ICollection<DocumentWorkflowStep> WorkflowSteps { get; set; } = new List<DocumentWorkflowStep>();
}
