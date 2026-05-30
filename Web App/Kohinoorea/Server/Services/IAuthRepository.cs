using Kohinoorea.Shared.Models.Auth;

namespace Kohinoorea.Server.Services;

public interface IAuthRepository
{
    Task<UserAccount?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUserDto>> GetAdminUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUserDto>> GetAdminsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetActiveUserEmailsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminLeadNotificationDto>> GetFollowUpCandidatesAsync(CancellationToken cancellationToken = default);

    Task<AdminLeadNotificationDto?> GetFollowUpCandidateAsync(long userId, CancellationToken cancellationToken = default);

    Task<bool> SetUserActiveAsync(long userId, bool isActive, CancellationToken cancellationToken = default);

    Task<long> CreateUserAsync(SignupRequest request, string passwordHash, CancellationToken cancellationToken = default);

    Task<long> CreateAdminAsync(CreateAdminRequest request, string passwordHash, CancellationToken cancellationToken = default);

    Task<bool> UpdateAdminAsync(long adminUserId, UpdateAdminRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateUserPasswordHashAsync(long userId, string passwordHash, CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(long userId, CancellationToken cancellationToken = default);

    Task<long> CreateSignupSubmissionAsync(SignupRequest request, CancellationToken cancellationToken = default);

    Task UpdateLastLoginAsync(long userId, DateTime lastLoginAtUtc, CancellationToken cancellationToken = default);
}
