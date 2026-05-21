namespace Kohinoorea.Shared.Models.Commerce;

public sealed class UserOrderDto
{
    public long OrderId { get; set; }

    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public DateTime OrderedAtUtc { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = "Card";

    public string Status { get; set; } = "Pending";
}
