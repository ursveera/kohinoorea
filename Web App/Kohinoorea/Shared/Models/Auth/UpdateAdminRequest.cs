using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Auth;

public sealed class UpdateAdminRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public List<string> Roles { get; set; } = new();
}
