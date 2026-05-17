using Kohinoorea.Shared.Models.Auth;
using SqlKata.Execution;

namespace Kohinoorea.Server.Services;

public sealed class AuthRepository : IAuthRepository
{
    private readonly QueryFactory _queryFactory;

    public AuthRepository(QueryFactory queryFactory)
    {
        _queryFactory = queryFactory;
    }

    public async Task<UserAccount?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _queryFactory
            .Query(AuthSqlKataSchema.UsersTable)
            .Where(AuthSqlKataSchema.UserColumns.Email, normalizedEmail)
            .FirstOrDefaultAsync<UserAccount>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetAdminUsersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _queryFactory
            .Query(AuthSqlKataSchema.UsersTable)
            .SelectRaw("id as Id")
            .SelectRaw("full_name as FullName")
            .SelectRaw("email as Email")
            .SelectRaw("phone as Phone")
            .SelectRaw("mt4_broker as Mt4Broker")
            .SelectRaw("role as Role")
            .SelectRaw("is_active as IsActive")
            .SelectRaw("created_at_utc as CreatedAtUtc")
            .SelectRaw("last_login_at_utc as LastLoginAtUtc")
            .OrderByDesc(AuthSqlKataSchema.UserColumns.CreatedAtUtc)
            .GetAsync<AdminUserDto>(cancellationToken: cancellationToken);

        return rows.ToList();
    }

    public async Task<long> CreateUserAsync(SignupRequest request, string passwordHash, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var normalizedBroker = string.IsNullOrWhiteSpace(request.Mt4Broker) ? null : request.Mt4Broker.Trim();

        return await _queryFactory
            .Query(AuthSqlKataSchema.UsersTable)
            .InsertGetIdAsync<long>(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.UserColumns.FullName] = request.FullName.Trim(),
                [AuthSqlKataSchema.UserColumns.Email] = normalizedEmail,
                [AuthSqlKataSchema.UserColumns.Phone] = normalizedPhone,
                [AuthSqlKataSchema.UserColumns.Mt4Broker] = normalizedBroker,
                [AuthSqlKataSchema.UserColumns.PasswordHash] = passwordHash,
                [AuthSqlKataSchema.UserColumns.Role] = AuthRoles.User,
                [AuthSqlKataSchema.UserColumns.IsActive] = true,
                [AuthSqlKataSchema.UserColumns.CreatedAtUtc] = now
            }, cancellationToken: cancellationToken);
    }

    public async Task<long> CreateSignupSubmissionAsync(SignupRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var normalizedBroker = string.IsNullOrWhiteSpace(request.Mt4Broker) ? null : request.Mt4Broker.Trim();

        return await _queryFactory
            .Query(AuthSqlKataSchema.SignupSubmissionsTable)
            .InsertGetIdAsync<long>(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.SignupColumns.FullName] = request.FullName.Trim(),
                [AuthSqlKataSchema.SignupColumns.Email] = normalizedEmail,
                [AuthSqlKataSchema.SignupColumns.Phone] = normalizedPhone,
                [AuthSqlKataSchema.SignupColumns.Mt4Broker] = normalizedBroker,
                [AuthSqlKataSchema.SignupColumns.AccessPlan] = request.AccessPlan,
                [AuthSqlKataSchema.SignupColumns.Notes] = request.Notes,
                [AuthSqlKataSchema.SignupColumns.CreatedAtUtc] = now
            }, cancellationToken: cancellationToken);
    }

    public async Task UpdateLastLoginAsync(long userId, DateTime lastLoginAtUtc, CancellationToken cancellationToken = default)
    {
        await _queryFactory
            .Query(AuthSqlKataSchema.UsersTable)
            .Where(AuthSqlKataSchema.UserColumns.Id, userId)
            .UpdateAsync(new Dictionary<string, object?>
            {
                [AuthSqlKataSchema.UserColumns.LastLoginAtUtc] = lastLoginAtUtc
            }, cancellationToken: cancellationToken);
    }
}
