using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CreateCheckoutRequest
{
    // Optional override: Stripe | PayPal (defaults to server config)
    public string? Provider { get; set; }

    [Required]
    public List<CheckoutItem> Items { get; set; } = new();

    public sealed class CheckoutItem
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
