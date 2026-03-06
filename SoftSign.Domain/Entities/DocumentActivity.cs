using SoftSign.Domain.Common;

namespace SoftSign.Domain.Entities;

public class DocumentActivity : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public virtual Document Document { get; set; } = null!;
    public virtual User? User { get; set; }
}
