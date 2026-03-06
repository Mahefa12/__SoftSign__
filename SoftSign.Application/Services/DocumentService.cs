using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Enums;
using SoftSign.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SoftSign.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IRepository<Document> _documentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DocumentService(IRepository<Document> documentRepository, IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid id)
    {
        // Use Query with Include to eagerly load SignatureZones and Workflow
        var document = await _documentRepository.Query()
            .Include(d => d.SignatureZones)
            .Include(d => d.Workflow)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (document == null) return null;
        return MapToDto(document);
    }

    public async Task<Document?> GetEntityByIdAsync(Guid id)
    {
        return await _documentRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<DocumentDto>> GetAllAsync(Guid? companyId = null)
    {
        IEnumerable<Document> documents;
        if (companyId.HasValue)
            documents = await _documentRepository.FindAsync(d => d.CompanyId == companyId.Value);
        else
            documents = await _documentRepository.GetAllAsync();

        return documents.Select(MapToDto);
    }

    public async Task<IEnumerable<DocumentDto>> GetByUserAsync(Guid userId)
    {
        var documents = await _documentRepository.FindAsync(d => d.CreatedById == userId);
        return documents.Select(MapToDto);
    }

    public async Task<DocumentDto> CreateAsync(CreateDocumentDto dto, Guid userId)
    {
        var document = new Document
        {
            Title = dto.Title,
            Description = dto.Description,
            CompanyId = dto.CompanyId,
            WorkflowId = dto.WorkflowId,
            CreatedById = userId,
            ExpiresAt = dto.ExpiresAt,
            Status = DocumentStatus.Draft,
            // Save file info from the DTO
            FileName = dto.FileName ?? string.Empty,
            OriginalFileName = dto.OriginalFileName ?? string.Empty,
            ContentType = dto.ContentType ?? "application/pdf",
            FileSize = dto.FileSize ?? 0
        };

        await _documentRepository.AddAsync(document);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(document);
    }

    public async Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto)
    {
        var document = await _documentRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Document not found");

        document.Title = dto.Title;
        document.Description = dto.Description;
        document.ExpiresAt = dto.ExpiresAt;
        document.UpdatedAt = DateTime.UtcNow;

        _documentRepository.Update(document);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(document);
    }

    public async Task DeleteAsync(Guid id)
    {
        var document = await _documentRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Document not found");

        document.IsDeleted = true;
        _documentRepository.Update(document);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeletePermanentlyAsync(Guid id)
    {
        var document = await _documentRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Document not found");

        // Delete all related records first (cascade delete)
        // These are handled by EF Core cascade delete if configured, but we do it explicitly
        // to ensure file paths are available for deletion
        
        // Get related data to extract file paths before deletion
        var signatureZones = document.SignatureZones?.ToList() ?? new List<SignatureZone>();
        var activities = document.Activities?.ToList() ?? new List<DocumentActivity>();
        var signatures = document.Signatures?.ToList() ?? new List<DocumentSignature>();
        var versions = document.Versions?.ToList() ?? new List<DocumentVersion>();

        // Remove related entities
        foreach (var zone in signatureZones)
        {
            _unitOfWork.GetRepository<SignatureZone>().Remove(zone);
        }
        foreach (var activity in activities)
        {
            _unitOfWork.GetRepository<DocumentActivity>().Remove(activity);
        }
        foreach (var signature in signatures)
        {
            _unitOfWork.GetRepository<DocumentSignature>().Remove(signature);
        }
        foreach (var version in versions)
        {
            _unitOfWork.GetRepository<DocumentVersion>().Remove(version);
        }

        // Now remove the document
        _documentRepository.Remove(document);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<DocumentDto> UpdateStatusAsync(Guid id, int status)
    {
        var document = await _documentRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Document not found");

        document.Status = (DocumentStatus)status;
        document.UpdatedAt = DateTime.UtcNow;

        if (document.Status == DocumentStatus.Completed)
            document.SignedAt = DateTime.UtcNow;

        _documentRepository.Update(document);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(document);
    }

    public async Task<IEnumerable<DocumentDto>> GetPendingSignaturesAsync(Guid userId)
    {
        var documents = await _documentRepository.FindAsync(d =>
            d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.InProgress);
        return documents.Select(MapToDto);
    }

    public async Task UpdateSignedFilePathAsync(Guid id, string signedFilePath)
    {
        var document = await _documentRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Document not found");

        document.SignedFilePath = signedFilePath;
        document.UpdatedAt = DateTime.UtcNow;
        
        _documentRepository.Update(document);
        await _unitOfWork.SaveChangesAsync();
    }

    private static DocumentDto MapToDto(Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            FileName = document.FileName,
            OriginalFileName = document.OriginalFileName,
            SignedFilePath = document.SignedFilePath,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            Version = document.Version,
            Status = document.Status,
            CompanyId = document.CompanyId,
            CreatedById = document.CreatedById,
            CreatedByName = document.CreatedBy != null ? $"{document.CreatedBy.FirstName} {document.CreatedBy.LastName}" : "",
            CompanyName = document.Company?.Name,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            ExpiresAt = document.ExpiresAt,
            SignedAt = document.SignedAt,
            // Use SignatureZones for counts and list
            SignatureCount = document.SignatureZones?.Count ?? 0,
            CompletedSignatureCount = document.SignatureZones?.Count(z => z.IsRequired && z.IsSigned) ?? 0,
            SignatureZones = document.SignatureZones?.ToList(),
            // Workflow fields
            WorkflowId = document.WorkflowId,
            WorkflowName = document.Workflow?.Name,
            IsWorkflowCustomized = document.IsWorkflowCustomized
        };
    }
}
