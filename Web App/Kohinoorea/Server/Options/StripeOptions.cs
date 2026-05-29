namespace Kohinoorea.Server.Options;

public sealed class StripeOptions
{
    public string? Mode { get; set; } = "Test"; // Test | Live

    public string? TestSecretKey { get; set; }
    public string? LiveSecretKey { get; set; }

    public string? TestWebhookSecret { get; set; }
    public string? LiveWebhookSecret { get; set; }

    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

