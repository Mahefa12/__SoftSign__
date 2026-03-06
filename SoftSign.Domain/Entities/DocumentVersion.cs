using SoftSign.Domain.Common;

namespace SoftSign.Domain.Entities;

public class DocumentVersion : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ChangeDescription { get; set; }
    public Guid CreatedById { get; set; }

    public virtual Document Document { get; set; } = null!;
    public virtual User CreatedBy { get; set; } = null!;
}
