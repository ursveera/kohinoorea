namespace Kohinoorea.Shared.Models.Commerce;

public sealed class SupportQueryDto
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "Open";

    public DateTime CreatedAtUtc { get; set; }
}
