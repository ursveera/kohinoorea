using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Commerce;

public sealed class CloseSupportTicketRequest
{
    [StringLength(500)]
    public string? Message { get; set; }
}

