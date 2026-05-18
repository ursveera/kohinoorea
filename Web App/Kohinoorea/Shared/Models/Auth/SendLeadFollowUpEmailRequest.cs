using System.ComponentModel.DataAnnotations;

namespace Kohinoorea.Shared.Models.Auth;

public sealed class SendLeadFollowUpEmailRequest
{
    [Required]
    [StringLength(160, MinimumLength = 3)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;
}
