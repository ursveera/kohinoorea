using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Auth;

public sealed class EmailOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

