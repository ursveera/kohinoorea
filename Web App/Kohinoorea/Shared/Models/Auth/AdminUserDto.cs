namespace Kohinoorea.Shared.Models.Auth;

public sealed class AdminUserDto
{
    public long Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Mt4Broker { get; set; }

    public string Role { get; set; } = AuthRoles.User;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }
}
