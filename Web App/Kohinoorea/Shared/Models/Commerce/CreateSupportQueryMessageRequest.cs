using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CreateSupportQueryMessageRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}
