using SoftSign.Domain.Enums;

namespace SoftSign.Application.DTOs;

public class CreateZoneDto
{
    public Guid DocumentId { get; set; }
    public int PageNumber { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    
    // Use string to accept JSON values like "Signature", "Initial", "Paraphe"
    public string Level { get; set; } = "Signature";
    
    public string? Label { get; set; }
    public Guid? AssignedUserId { get; set; }
    public bool IsRequired { get; set; } = true;
    public int Order { get; set; }
}

public class ValidateZoneDto
{
    public double SignatureX { get; set; }
    public double SignatureY { get; set; }
    public double SignatureWidth { get; set; }
    public double SignatureHeight { get; set; }
    public double ZoneX { get; set; }
    public double ZoneY { get; set; }
    public double ZoneWidth { get; set; }
    public double ZoneHeight { get; set; }
}
