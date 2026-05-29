using System.Net;
using System.Net.Mail;

namespace Kohinoorea.Server.Services;

public sealed class SmtpEmailDeliveryService : IEmailDeliveryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailDeliveryService> _logger;

    public SmtpEmailDeliveryService(IConfiguration configuration, ILogger<SmtpEmailDeliveryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendPlainTextEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        var normalizedEmail = toEmail?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return (false, "Recipient email address is required.");
        }

        var normalizedSubject = subject?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSubject))
        {
            return (false, "Email subject is required.");
        }

        var normalizedBody = body?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBody))
        {
            return (false, "Email message is required.");
        }

        var smtpHost = _configuration["Smtp:Host"];
        var fromAddress = _configuration["Smtp:From"];
        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("SMTP is not configured (missing Smtp:Host or Smtp:From). Email to {Email} was skipped.", normalizedEmail);
            return (false, "Email service is not configured yet. Please update the SMTP settings.");
        }

        try
        {
            using var client = BuildSmtpClient(smtpHost);
            using var message = new MailMessage(fromAddress, normalizedEmail)
            {
                Subject = normalizedSubject,
                Body = normalizedBody,
                IsBodyHtml = false
            };

            await client.SendMailAsync(message, cancellationToken);
            return (true, "Email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", normalizedEmail);
            return (false, "Unable to send email right now. Please try again.");
        }
    }

    public async Task<(bool Success, string Message)> SendHtmlEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var normalizedEmail = toEmail?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return (false, "Recipient email address is required.");
        }

        var normalizedSubject = subject?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSubject))
        {
            return (false, "Email subject is required.");
        }

        var normalizedBody = htmlBody?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBody))
        {
            return (false, "Email message is required.");
        }

        var smtpHost = _configuration["Smtp:Host"];
        var fromAddress = _configuration["Smtp:From"];
        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("SMTP is not configured (missing Smtp:Host or Smtp:From). Email to {Email} was skipped.", normalizedEmail);
            return (false, "Email service is not configured yet. Please update the SMTP settings.");
        }

        try
        {
            using var client = BuildSmtpClient(smtpHost);
            using var message = new MailMessage(fromAddress, normalizedEmail)
            {
                Subject = normalizedSubject,
                Body = normalizedBody,
                IsBodyHtml = true
            };

            await client.SendMailAsync(message, cancellationToken);
            return (true, "Email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", normalizedEmail);
            return (false, "Unable to send email right now. Please try again.");
        }
    }

    private SmtpClient BuildSmtpClient(string host)
    {
        var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var enableSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var parsedSsl) || parsedSsl;
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];

        var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password ?? string.Empty);
        }

        return client;
    }
}
