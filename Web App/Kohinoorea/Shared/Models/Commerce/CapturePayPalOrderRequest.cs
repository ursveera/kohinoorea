using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CapturePayPalOrderRequest
{
    [Required]
    public string OrderId { get; set; } = string.Empty;
}

