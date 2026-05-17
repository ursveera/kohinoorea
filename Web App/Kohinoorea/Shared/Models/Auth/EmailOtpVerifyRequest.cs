using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Auth;

public sealed class EmailOtpVerifyRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(10, MinimumLength = 4)]
    public string Otp { get; set; } = string.Empty;
}

