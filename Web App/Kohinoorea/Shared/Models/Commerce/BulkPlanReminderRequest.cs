using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class BulkPlanReminderRequest
{
    [Required]
    public List<long> OrderIds { get; set; } = new();
}

