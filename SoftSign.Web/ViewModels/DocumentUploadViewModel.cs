using Microsoft.AspNetCore.Http;

namespace SoftSign.Web.ViewModels;

public class DocumentUploadViewModel
{
    public string DocumentName { get; set; } = string.Empty;
    public Guid? DocumentTypeId { get; set; }
    public Guid? WorkflowId { get; set; }
    public IFormFile? File { get; set; }
    public List<SignatureZoneViewModel> GeneratedZones { get; set; } = new List<SignatureZoneViewModel>();
}
