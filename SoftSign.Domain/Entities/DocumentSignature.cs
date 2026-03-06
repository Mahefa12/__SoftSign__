using SoftSign.Domain.Common;
using SoftSign.Domain.Enums;

namespace SoftSign.Domain.Entities;

public class DocumentSignature : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid SignerId { get; set; }
    public SignatureLevel Level { get; set; } = SignatureLevel.Signature;
    public string? SignatureImagePath { get; set; }
    public string? SignatureData { get; set; }
    public bool IsSigned { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Comments { get; set; }
    public int? PageNumber { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }

    public virtual Document Document { get; set; } = null!;
    public virtual User Signer { get; set; } = null!;
}
