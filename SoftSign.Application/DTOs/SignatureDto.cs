using SoftSign.Domain.Enums;

namespace SoftSign.Application.DTOs;

public class SignatureDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid SignerId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public SignatureLevel Level { get; set; }
    public bool IsSigned { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? Comments { get; set; }
    public int? PageNumber { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}

public class CreateSignatureDto
{
    public Guid DocumentId { get; set; }
    public Guid SignerId { get; set; }
    public SignatureLevel Level { get; set; } = SignatureLevel.Signature;
    public int? PageNumber { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}

public class ApplySignatureDto
{
    public Guid SignatureId { get; set; }
    public string? SignatureData { get; set; }
    public string? Comments { get; set; }
}
