using SoftSign.Domain.Common;

namespace SoftSign.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? Logo { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
