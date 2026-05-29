namespace Kohinoorea.Server.Services;

public interface IEmailDeliveryService
{
    Task<(bool Success, string Message)> SendPlainTextEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
    Task<(bool Success, string Message)> SendHtmlEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken);
}
