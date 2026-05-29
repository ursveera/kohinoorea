namespace Kohinoorea.Server.Options;

public sealed class PaymentOptions
{
    // Stripe | PayPal
    public string? Provider { get; set; } = "Stripe";
}

