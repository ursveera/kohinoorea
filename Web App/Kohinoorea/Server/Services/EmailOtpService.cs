using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Kohinoorea.Server.Services;

public sealed class EmailOtpService : IEmailOtpService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan OtpCooldown = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan VerifiedLifetime = TimeSpan.FromMinutes(15);
    private const string CachePrefix = "email-otp:";

    private readonly IMemoryCache _cache;
    private readonly IEmailDeliveryService _emailDeliveryService;
    private readonly ILogger<EmailOtpService> _logger;

    public EmailOtpService(IMemoryCache cache, IEmailDeliveryService emailDeliveryService, ILogger<EmailOtpService> logger)
    {
        _cache = cache;
        _emailDeliveryService = emailDeliveryService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendOtpAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return (false, "Invalid email address.");
        }

        var cooldownKey = $"{CachePrefix}{normalizedEmail}:cooldown";
        if (_cache.TryGetValue(cooldownKey, out _))
        {
            return (false, "Please wait a moment before requesting another OTP.");
        }

        var otp = GenerateOtp();
        var otpHash = HashOtp(otp);
        var verifiedKey = $"{CachePrefix}{normalizedEmail}:verified";
        var subject = "Kohinoor EA - Email verification OTP";
        var body = $"Your verification code is: {otp}\n\nThis code expires in 10 minutes.";

        try
        {
            var (success, message) = await _emailDeliveryService.SendPlainTextEmailAsync(normalizedEmail, subject, body, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("OTP email delivery failed for {Email}. Generated OTP was not stored.", normalizedEmail);
                return (false, message);
            }

            _cache.Set($"{CachePrefix}{normalizedEmail}:hash", otpHash, OtpLifetime);
            _cache.Set($"{CachePrefix}{normalizedEmail}:attempts", 0, OtpLifetime);
            _cache.Set(cooldownKey, true, OtpCooldown);
            _cache.Remove(verifiedKey);
            return (true, "OTP sent to your email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", normalizedEmail);
            return (false, "Unable to send OTP right now. Please try again.");
        }
    }

    public Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otp, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Task.FromResult<(bool, string)>((false, "Invalid email address."));
        }

        if (string.IsNullOrWhiteSpace(otp))
        {
            return Task.FromResult<(bool, string)>((false, "Please enter the OTP."));
        }

        var attemptsKey = $"{CachePrefix}{normalizedEmail}:attempts";
        var otpHashKey = $"{CachePrefix}{normalizedEmail}:hash";

        if (!_cache.TryGetValue(otpHashKey, out string? expectedHash) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return Task.FromResult<(bool, string)>((false, "OTP expired. Please request a new code."));
        }

        var attempts = _cache.TryGetValue(attemptsKey, out int currentAttempts) ? currentAttempts : 0;
        if (attempts >= 5)
        {
            return Task.FromResult<(bool, string)>((false, "Too many attempts. Please request a new code."));
        }

        var providedHash = HashOtp(otp.Trim());
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedHash), Encoding.UTF8.GetBytes(providedHash)))
        {
            _cache.Set(attemptsKey, attempts + 1, OtpLifetime);
            return Task.FromResult<(bool, string)>((false, "Invalid OTP."));
        }

        _cache.Remove(otpHashKey);
        _cache.Remove(attemptsKey);
        _cache.Set($"{CachePrefix}{normalizedEmail}:verified", true, VerifiedLifetime);
        return Task.FromResult<(bool, string)>((true, "Email verified."));
    }

    public Task<bool> IsEmailVerifiedAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Task.FromResult(false);
        }

        var verified = _cache.TryGetValue($"{CachePrefix}{normalizedEmail}:verified", out bool isVerified) && isVerified;
        return Task.FromResult(verified);
    }

    public Task ClearVerifiedEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            _cache.Remove($"{CachePrefix}{normalizedEmail}:verified");
        }

        return Task.CompletedTask;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string GenerateOtp()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 1_000_000;
        return value.ToString("D6");
    }

    private static string HashOtp(string otp)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(otp));
        return Convert.ToHexString(bytes);
    }
}
