namespace SoftSign.Application.DTOs;

public class ZonePositionDto
{
    public Guid ZoneId { get; set; }
    public Guid? StepId { get; set; }
    public string? SignerRole { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}
