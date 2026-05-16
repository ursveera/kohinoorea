using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class UpdateOrderStatusRequest
{
    [Required]
    [RegularExpression("^(Pending|Completed|Denied)$", ErrorMessage = "Status must be Pending, Completed, or Denied.")]
    public string Status { get; set; } = "Pending";
}
