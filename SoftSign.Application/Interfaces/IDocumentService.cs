using SoftSign.Application.DTOs;
using SoftSign.Domain.Entities;

namespace SoftSign.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentDto?> GetByIdAsync(Guid id);
    Task<Document?> GetEntityByIdAsync(Guid id);
    Task<IEnumerable<DocumentDto>> GetAllAsync(Guid? companyId = null);
    Task<IEnumerable<DocumentDto>> GetByUserAsync(Guid userId);
    Task<DocumentDto> CreateAsync(CreateDocumentDto dto, Guid userId);
    Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto);
    Task DeleteAsync(Guid id);
    Task DeletePermanentlyAsync(Guid id);
    Task<DocumentDto> UpdateStatusAsync(Guid id, int status);
    Task UpdateSignedFilePathAsync(Guid id, string signedFilePath);
    Task<IEnumerable<DocumentDto>> GetPendingSignaturesAsync(Guid userId);
}
