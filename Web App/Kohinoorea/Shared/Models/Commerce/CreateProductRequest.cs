using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CreateProductRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(2000)]
    public string? ImageLink { get; set; }

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsMaster { get; set; } = false;

    [StringLength(5)]
    public string? CountryCode { get; set; }

    // Optional plan validity window (UTC).
    // If set, an order for this product can be treated as active within this range.
    public DateTime? ValidFromUtc { get; set; }

    public DateTime? ValidToUtc { get; set; }
}
