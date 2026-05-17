namespace Kohinoorea.Server.Services;

public interface IEmailOtpService
{
    Task<(bool Success, string Message)> SendOtpAsync(string email, CancellationToken cancellationToken);

    Task<(bool Success, string Message)> VerifyOtpAsync(string email, string otp, CancellationToken cancellationToken);

    Task<bool> IsEmailVerifiedAsync(string email, CancellationToken cancellationToken);

    Task ClearVerifiedEmailAsync(string email, CancellationToken cancellationToken);
}
