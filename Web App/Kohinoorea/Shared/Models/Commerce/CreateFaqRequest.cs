using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CreateFaqRequest
{
    [Required]
    [StringLength(250, MinimumLength = 5)]
    public string Question { get; set; } = string.Empty;

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public string Answer { get; set; } = string.Empty;

    [Range(0, 1000)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
