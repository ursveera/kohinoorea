namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CreateStripeCheckoutRequest
{
    public List<StripeCheckoutItem> Items { get; set; } = new();
}

public sealed class StripeCheckoutItem
{
    public long ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

