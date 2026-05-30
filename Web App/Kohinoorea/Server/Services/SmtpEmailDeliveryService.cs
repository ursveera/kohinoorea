using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.FileProviders;

namespace Kohinoorea.Server.Services;

public sealed class SmtpEmailDeliveryService : IEmailDeliveryService, IOrderEmailDeliveryService, ISupportEmailDeliveryService, IAdminEmailDeliveryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailDeliveryService> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly string _settingsPrefix;

    public SmtpEmailDeliveryService(
        IConfiguration configuration,
        ILogger<SmtpEmailDeliveryService> logger,
        IHostEnvironment? hostEnvironment = null,
        string settingsPrefix = "Smtp")
    {
        _configuration = configuration;
        _logger = logger;
        _hostEnvironment = hostEnvironment ?? new FallbackHostEnvironment();
        _settingsPrefix = string.IsNullOrWhiteSpace(settingsPrefix) ? "Smtp" : settingsPrefix.Trim();
    }

    private sealed class FallbackHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
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

        var smtpHost = _configuration[$"{_settingsPrefix}:Host"];
        var fromAddress = _configuration[$"{_settingsPrefix}:From"];
        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning(
                "SMTP is not configured (missing {Prefix} Host or {Prefix} From). Email to {Email} was skipped.",
                _settingsPrefix,
                _settingsPrefix,
                normalizedEmail);
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
            return _hostEnvironment.IsDevelopment()
                ? (false, $"Unable to send email: {FormatExceptionForClient(ex)}")
                : (false, "Unable to send email right now. Please try again.");
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

        var smtpHost = _configuration[$"{_settingsPrefix}:Host"];
        var fromAddress = _configuration[$"{_settingsPrefix}:From"];
        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning(
                "SMTP is not configured (missing {Prefix} Host or {Prefix} From). Email to {Email} was skipped.",
                _settingsPrefix,
                _settingsPrefix,
                normalizedEmail);
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
            return _hostEnvironment.IsDevelopment()
                ? (false, $"Unable to send email: {FormatExceptionForClient(ex)}")
                : (false, "Unable to send email right now. Please try again.");
        }
    }

    private static string FormatExceptionForClient(Exception ex)
    {
        if (ex is SmtpException smtpEx)
        {
            var status = smtpEx.StatusCode != SmtpStatusCode.GeneralFailure
                ? $"SMTP status: {smtpEx.StatusCode}. "
                : string.Empty;

            var inner = smtpEx.InnerException is not null
                ? $" Inner: {smtpEx.InnerException.GetType().Name}: {smtpEx.InnerException.Message}"
                : string.Empty;

            return $"{status}{smtpEx.Message}{inner}";
        }

        var fallbackInner = ex.InnerException is not null
            ? $" Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
            : string.Empty;

        return $"{ex.GetType().Name}: {ex.Message}{fallbackInner}";
    }

    private SmtpClient BuildSmtpClient(string host)
    {
        var port = int.TryParse(_configuration[$"{_settingsPrefix}:Port"], out var parsedPort) ? parsedPort : 587;
        var enableSsl = !bool.TryParse(_configuration[$"{_settingsPrefix}:EnableSsl"], out var parsedSsl) || parsedSsl;
        var username = _configuration[$"{_settingsPrefix}:Username"];
        var password = _configuration[$"{_settingsPrefix}:Password"];

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
