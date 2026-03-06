using SoftSign.Domain.Common;
using SoftSign.Domain.Enums;

namespace SoftSign.Domain.Entities;

public class SignatureZone : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int PageNumber { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public SignatureLevel Level { get; set; } = SignatureLevel.Signature;
    public string? Label { get; set; }
    public Guid? AssignedUserId { get; set; }
    public bool IsRequired { get; set; } = true;
    public int Order { get; set; }

    // Signature tracking fields
    public string? SignatureImage { get; set; }  // Base64 PNG encoded signature
    public Guid? SignedByUserId { get; set; }    // User who signed this zone
    public DateTime? SignedAt { get; set; }       // When the signature was applied

    // Computed property to check if zone is signed
    public bool IsSigned => !string.IsNullOrEmpty(SignatureImage);

    public virtual Document Document { get; set; } = null!;
    public virtual User? AssignedUser { get; set; }
    public virtual User? SignedByUser { get; set; }
}
