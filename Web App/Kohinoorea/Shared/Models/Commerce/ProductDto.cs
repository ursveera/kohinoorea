namespace Kohinoorea.Shared.Models.Commerce;

public sealed class ProductDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageLink { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public bool IsMaster { get; set; }

    public string? CountryCode { get; set; }

    public DateTime? ValidFromUtc { get; set; }

    public DateTime? ValidToUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
