using SoftSign.Application.DTOs;
using SoftSign.Application.Interfaces;
using SoftSign.Domain.Entities;
using SoftSign.Domain.Interfaces;

namespace SoftSign.Application.Services;

public class SignatureService : ISignatureService
{
    private readonly IRepository<DocumentSignature> _signatureRepository;
    private readonly IRepository<Document> _documentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SignatureService(
        IRepository<DocumentSignature> signatureRepository,
        IRepository<Document> documentRepository,
        IUnitOfWork unitOfWork)
    {
        _signatureRepository = signatureRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SignatureDto?> GetByIdAsync(Guid id)
    {
        var signature = await _signatureRepository.GetByIdAsync(id);
        if (signature == null) return null;
        return MapToDto(signature);
    }

    public async Task<IEnumerable<SignatureDto>> GetByDocumentAsync(Guid documentId)
    {
        var signatures = await _signatureRepository.FindAsync(s => s.DocumentId == documentId);
        return signatures.Select(MapToDto);
    }

    public async Task<SignatureDto> CreateAsync(CreateSignatureDto dto)
    {
        var signature = new DocumentSignature
        {
            DocumentId = dto.DocumentId,
            SignerId = dto.SignerId,
            Level = dto.Level,
            PageNumber = dto.PageNumber,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            Width = dto.Width,
            Height = dto.Height
        };

        await _signatureRepository.AddAsync(signature);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(signature);
    }

    public async Task<SignatureDto> ApplySignatureAsync(ApplySignatureDto dto, string ipAddress, string userAgent)
    {
        var signature = await _signatureRepository.GetByIdAsync(dto.SignatureId)
            ?? throw new InvalidOperationException("Signature not found");

        signature.SignatureData = dto.SignatureData;
        signature.Comments = dto.Comments;
        signature.IsSigned = true;
        signature.SignedAt = DateTime.UtcNow;
        signature.IpAddress = ipAddress;
        signature.UserAgent = userAgent;

        _signatureRepository.Update(signature);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(signature);
    }

    public async Task DeleteAsync(Guid id)
    {
        var signature = await _signatureRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Signature not found");

        _signatureRepository.Remove(signature);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> HasUserSignedAsync(Guid documentId, Guid userId)
    {
        return await _signatureRepository.AnyAsync(s =>
            s.DocumentId == documentId && s.SignerId == userId && s.IsSigned);
    }

    private static SignatureDto MapToDto(DocumentSignature signature)
    {
        return new SignatureDto
        {
            Id = signature.Id,
            DocumentId = signature.DocumentId,
            SignerId = signature.SignerId,
            SignerName = signature.Signer != null ? $"{signature.Signer.FirstName} {signature.Signer.LastName}" : "",
            Level = signature.Level,
            IsSigned = signature.IsSigned,
            SignedAt = signature.SignedAt,
            Comments = signature.Comments,
            PageNumber = signature.PageNumber,
            PositionX = signature.PositionX,
            PositionY = signature.PositionY,
            Width = signature.Width,
            Height = signature.Height
        };
    }
}
