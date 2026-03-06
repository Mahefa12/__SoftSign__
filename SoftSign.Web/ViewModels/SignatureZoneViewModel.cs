namespace SoftSign.Web.ViewModels;

public class SignatureZoneViewModel
{
    public Guid ZoneId { get; set; }
    public Guid StepId { get; set; }
    public string SignerRole { get; set; } = string.Empty;
    public int ZoneWidth { get; set; } = 200; // Default 200px
    public int ZoneHeight { get; set; } = 80;  // Default 80px
}
