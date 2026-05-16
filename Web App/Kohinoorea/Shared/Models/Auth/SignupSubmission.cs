namespace Kohinoorea.Shared.Models.Auth;

public sealed class SignupSubmission
{
    public long Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Mt4Broker { get; set; } = string.Empty;

    public string AccessPlan { get; set; } = "demo";

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
