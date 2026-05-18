using Kohinoorea.Shared.Models.Auth;

namespace Kohinoorea.Server.Services;

public interface IAuthRepository
{
    Task<UserAccount?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUserDto>> GetAdminUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminLeadNotificationDto>> GetFollowUpCandidatesAsync(CancellationToken cancellationToken = default);

    Task<AdminLeadNotificationDto?> GetFollowUpCandidateAsync(long userId, CancellationToken cancellationToken = default);

    Task<long> CreateUserAsync(SignupRequest request, string passwordHash, CancellationToken cancellationToken = default);

    Task<long> CreateSignupSubmissionAsync(SignupRequest request, CancellationToken cancellationToken = default);

    Task UpdateLastLoginAsync(long userId, DateTime lastLoginAtUtc, CancellationToken cancellationToken = default);
}
