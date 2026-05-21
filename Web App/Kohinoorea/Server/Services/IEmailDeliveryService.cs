namespace Kohinoorea.Server.Services;

public interface IEmailDeliveryService
{
    Task<(bool Success, string Message)> SendPlainTextEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
}
