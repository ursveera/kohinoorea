namespace Kohinoorea.Shared.Models.Auth;

public sealed class AdminLeadNotificationDto
{
    public long UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Mt4Broker { get; set; }

    public string? RequestedPlan { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public int TotalOrders { get; set; }

    public int CompletedOrders { get; set; }

    public DateTime? LastOrderAtUtc { get; set; }

    public string? LastOrderStatus { get; set; }
}
