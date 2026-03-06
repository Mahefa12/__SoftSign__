using SoftSign.Domain.Entities;

namespace SoftSign.Application.Interfaces;

public interface IPdfService
{
    Task<byte[]> ConvertToPdfAsync(Stream fileStream, string contentType);
    Task<byte[]> ApplySignatureOverlayAsync(byte[] pdfBytes, string signatureImageBase64, int pageNumber, double x, double y, double width, double height);
    Task<byte[]> ApplySignaturesFromZonesAsync(byte[] pdfBytes, IEnumerable<SignatureZone> zones);
    Task<int> GetPageCountAsync(byte[] pdfBytes);
    Task<(double Width, double Height)> GetPageSizeAsync(byte[] pdfBytes, int pageNumber = 1);
    Task<byte[]> MergePdfsAsync(IEnumerable<byte[]> pdfFiles);
}
