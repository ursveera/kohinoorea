namespace Kohinoorea.Shared.Models.Commerce;

public sealed class OrderTraceDto
{
    public long OrderId { get; set; }

    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal TotalAmount { get; set; }

    public long UserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string UserFullName { get; set; } = string.Empty;

    public DateTime OrderedAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;
}
