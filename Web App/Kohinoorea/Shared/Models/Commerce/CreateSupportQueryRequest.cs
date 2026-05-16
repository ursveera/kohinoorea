using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CreateSupportQueryRequest
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = "General";

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string Message { get; set; } = string.Empty;
}
