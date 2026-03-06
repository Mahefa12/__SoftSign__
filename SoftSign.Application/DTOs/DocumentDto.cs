using SoftSign.Domain.Enums;
using SoftSign.Domain.Entities;

namespace SoftSign.Application.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? SignedFilePath { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int Version { get; set; }
    public DocumentStatus Status { get; set; }
    public string StatusDisplay => Status switch
    {
        DocumentStatus.Draft => "Brouillon",
        DocumentStatus.Pending => "En attente",
        DocumentStatus.InProgress => "En cours",
        DocumentStatus.Completed => "Terminé",
        DocumentStatus.Rejected => "Rejeté",
        DocumentStatus.Expired => "Expiré",
        DocumentStatus.Cancelled => "Annulé",
        _ => Status.ToString()
    };
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public int SignatureCount { get; set; }
    public int CompletedSignatureCount { get; set; }
    public List<SignatureZone>? SignatureZones { get; set; }
    
    // Workflow fields
    public Guid? WorkflowId { get; set; }
    public string? WorkflowName { get; set; }
    public bool IsWorkflowCustomized { get; set; }
}

public class CreateDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? WorkflowId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    // File info - populated after file is saved
    public string? FileName { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
}

public class UpdateDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
