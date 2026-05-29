namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CreateCheckoutResponse
{
    public string Provider { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
}

