namespace SoftSign.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendSignatureRequestAsync(string to, string signerName, string documentTitle, string signatureLink);
    Task SendSignatureCompletedAsync(string to, string documentTitle);
    Task SendDocumentCompletedAsync(string to, string documentTitle, string downloadLink);
}
