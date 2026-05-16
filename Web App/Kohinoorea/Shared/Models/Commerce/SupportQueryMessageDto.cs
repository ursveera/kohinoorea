namespace Kohinoorea.Shared.Models.Commerce;

public sealed class SupportQueryMessageDto
{
    public long Id { get; set; }

    public long QueryId { get; set; }

    public string SenderRole { get; set; } = string.Empty;

    public long? SenderUserId { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
