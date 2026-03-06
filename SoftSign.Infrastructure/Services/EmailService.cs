using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using SoftSign.Application.Interfaces;

namespace SoftSign.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUser = _configuration["Email:SmtpUser"] ?? "";
        var smtpPass = _configuration["Email:SmtpPassword"] ?? "";
        var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@softsign.com";
        var fromName = _configuration["Email:FromName"] ?? "SoftSign";

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };
        message.To.Add(to);

        await client.SendMailAsync(message);
    }

    public async Task SendSignatureRequestAsync(string to, string signerName, string documentTitle, string signatureLink)
    {
        var subject = $"Signature Request: {documentTitle}";
        var body = $@"
            <h2>Signature Request</h2>
            <p>Dear {signerName},</p>
            <p>You have been requested to sign the document: <strong>{documentTitle}</strong></p>
            <p><a href='{signatureLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Sign Document</a></p>
            <p>Thank you,<br/>SoftSign Team</p>";

        await SendEmailAsync(to, subject, body);
    }

    public async Task SendSignatureCompletedAsync(string to, string documentTitle)
    {
        var subject = $"Signature Completed: {documentTitle}";
        var body = $@"
            <h2>Signature Completed</h2>
            <p>A signature has been applied to the document: <strong>{documentTitle}</strong></p>
            <p>Thank you,<br/>SoftSign Team</p>";

        await SendEmailAsync(to, subject, body);
    }

    public async Task SendDocumentCompletedAsync(string to, string documentTitle, string downloadLink)
    {
        var subject = $"Document Completed: {documentTitle}";
        var body = $@"
            <h2>Document Fully Signed</h2>
            <p>All signatures have been collected for: <strong>{documentTitle}</strong></p>
            <p><a href='{downloadLink}' style='background-color: #2196F3; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Download Document</a></p>
            <p>Thank you,<br/>SoftSign Team</p>";

        await SendEmailAsync(to, subject, body);
    }
}
