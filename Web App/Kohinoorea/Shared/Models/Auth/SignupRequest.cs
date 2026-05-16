using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Auth;

public sealed class SignupRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Mt4Broker { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [RegularExpression("^(demo|annual|lifetime)$")]
    public string AccessPlan { get; set; } = "demo";
}
