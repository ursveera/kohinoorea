using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class UpdateSupportQueryStatusRequest
{
    [Required]
    [RegularExpression("^(Open|Closed)$", ErrorMessage = "Status must be Open or Closed.")]
    public string Status { get; set; } = "Open";
}
