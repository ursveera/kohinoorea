namespace Kohinoorea.Shared.Models.Commerce;

public sealed class ContactMessageDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsReplied { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastRepliedAtUtc { get; set; }
}
