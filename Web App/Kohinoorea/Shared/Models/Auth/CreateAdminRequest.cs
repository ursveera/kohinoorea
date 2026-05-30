using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Auth;

public sealed class CreateAdminRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();
}
