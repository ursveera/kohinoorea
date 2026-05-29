namespace Kohinoorea.Server.Options;

public sealed class PayPalOptions
{
    public string? Mode { get; set; } = "Test"; // Test | Live

    // PayPal expects a 3-letter currency code like USD, INR, AUD...
    // Must be enabled/accepted on the seller account.
    public string? CurrencyCode { get; set; } = "USD";

    public string? TestClientId { get; set; }
    public string? TestClientSecret { get; set; }

    public string? LiveClientId { get; set; }
    public string? LiveClientSecret { get; set; }

    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}
