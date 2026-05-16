namespace Kohinoorea.Shared.Models.Commerce;

public sealed class OrderResultDto
{
    public long OrderId { get; set; }

    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime OrderedAtUtc { get; set; }
}
