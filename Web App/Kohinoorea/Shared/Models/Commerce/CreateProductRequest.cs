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
}
