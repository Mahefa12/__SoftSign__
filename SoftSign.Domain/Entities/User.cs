using Microsoft.AspNetCore.Identity;
using SoftSign.Domain.Common;

namespace SoftSign.Domain.Entities;

public class User : IdentityUser<Guid>, IBaseEntity
{
    public Guid? CompanyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public virtual Company? Company { get; set; }
    public virtual ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();
    public virtual ICollection<DocumentSignature> Signatures { get; set; } = new List<DocumentSignature>();
    public virtual ICollection<UserNotification> Notifications { get; set; } = new List<UserNotification>();
}
