using SoftSign.Application.DTOs;

namespace SoftSign.Application.Interfaces;

public interface ISignatureService
{
    Task<SignatureDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<SignatureDto>> GetByDocumentAsync(Guid documentId);
    Task<SignatureDto> CreateAsync(CreateSignatureDto dto);
    Task<SignatureDto> ApplySignatureAsync(ApplySignatureDto dto, string ipAddress, string userAgent);
    Task DeleteAsync(Guid id);
    Task<bool> HasUserSignedAsync(Guid documentId, Guid userId);
}
