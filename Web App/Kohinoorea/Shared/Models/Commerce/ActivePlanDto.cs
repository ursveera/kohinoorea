namespace Kohinoorea.Shared.Models.Commerce;

public sealed class ActivePlanDto
{
    public long OrderId { get; set; }
    public long UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;

    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public DateTime OrderedAtUtc { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }

    // Derived server-side
    public string PlanState { get; set; } = string.Empty; // Active | Expired | Upcoming | Unknown
    public string ReminderType { get; set; } = string.Empty; // None | ExpiringSoon | Expired
    public bool IsEligibleForReminder { get; set; }
}
