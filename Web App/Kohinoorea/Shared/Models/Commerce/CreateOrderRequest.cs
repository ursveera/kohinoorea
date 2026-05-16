using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CreateOrderRequest
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}
