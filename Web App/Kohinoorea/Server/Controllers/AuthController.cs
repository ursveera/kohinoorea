using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kohinoorea.Server.Services;
using Kohinoorea.Shared.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Kohinoorea.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher<string> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly IEmailOtpService _emailOtpService;
    private readonly IAdminEmailDeliveryService _adminEmailDeliveryService;

    public AuthController(
        IAuthRepository authRepository,
        IPasswordHasher<string> passwordHasher,
        IConfiguration configuration,
        IEmailOtpService emailOtpService,
        IAdminEmailDeliveryService adminEmailDeliveryService)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _emailOtpService = emailOtpService;
        _adminEmailDeliveryService = adminEmailDeliveryService;
    }

    [HttpPost("email-otp/send")]
    public async Task<ActionResult<AuthResponse>> SendEmailOtp([FromBody] EmailOtpRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse { Success = false, Message = "Invalid email request." });
        }

        var (success, message) = await _emailOtpService.SendOtpAsync(request.Email, cancellationToken);
        return Ok(new AuthResponse { Success = success, Message = message, Email = request.Email.Trim().ToLowerInvariant() });
    }

    [HttpPost("email-otp/verify")]
    public async Task<ActionResult<AuthResponse>> VerifyEmailOtp([FromBody] EmailOtpVerifyRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse { Success = false, Message = "Invalid OTP request." });
        }

        var (success, message) = await _emailOtpService.VerifyOtpAsync(request.Email, request.Otp, cancellationToken);
        return Ok(new AuthResponse { Success = success, Message = message, Email = request.Email.Trim().ToLowerInvariant() });
    }

    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> Signup([FromBody] SignupRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Invalid signup request."
            });
        }

        var existingUser = await _authRepository.GetUserByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Conflict(new AuthResponse
            {
                Success = false,
                Message = "An account with this email already exists."
            });
        }

        var isEmailVerified = await _emailOtpService.IsEmailVerifiedAsync(request.Email, cancellationToken);
        if (!isEmailVerified)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Please verify your email before signing up."
            });
        }

        var passwordHash = _passwordHasher.HashPassword(request.Email, request.Password);
        var userId = await _authRepository.CreateUserAsync(request, passwordHash, cancellationToken);
        await _authRepository.CreateSignupSubmissionAsync(request, cancellationToken);
        await _emailOtpService.ClearVerifiedEmailAsync(request.Email, cancellationToken);

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Signup completed successfully.",
            UserId = userId,
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = AuthRoles.User
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Invalid login request."
            });
        }

        var user = await _authRepository.GetUserByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password."
            });
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(request.Email, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password."
            });
        }

        await _authRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, cancellationToken);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(GetExpiryMinutes());
        var token = CreateToken(user, expiresAtUtc);

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Login successful.",
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            Roles = (string.IsNullOrWhiteSpace(user.Role) ? AuthRoles.User : user.Role.Trim())
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        });
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<AuthResponse> Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        long? userId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

        var roles = User.Claims
            .Where(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var role = roles.Contains(AuthRoles.Admin, StringComparer.OrdinalIgnoreCase)
            ? AuthRoles.Admin
            : roles.Contains(AuthRoles.User, StringComparer.OrdinalIgnoreCase) ? AuthRoles.User : roles.FirstOrDefault();

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Token is valid.",
            UserId = userId,
            Email = email,
            Role = role,
            Roles = roles
        });
    }

    [Authorize(Roles = "Admin,Admin.Customers")]
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _authRepository.GetAdminUsersAsync(cancellationToken);
        return Ok(users);
    }

    [Authorize(Roles = "Admin,Admin.Admins")]
    [HttpGet("admins")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetAdmins(CancellationToken cancellationToken)
    {
        var admins = await _authRepository.GetAdminsAsync(cancellationToken);
        return Ok(admins);
    }

    [Authorize(Roles = "Admin,Admin.Admins")]
    [HttpPost("admins")]
    public async Task<ActionResult<AuthResponse>> CreateAdmin([FromBody] CreateAdminRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse { Success = false, Message = "Invalid admin request." });
        }

        var existing = await _authRepository.GetUserByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            return Conflict(new AuthResponse { Success = false, Message = "An account with this email already exists." });
        }

        var passwordHash = _passwordHasher.HashPassword(request.Email.Trim().ToLowerInvariant(), request.Password);
        var id = await _authRepository.CreateAdminAsync(request, passwordHash, cancellationToken);
        return Ok(new AuthResponse { Success = true, Message = "Admin created.", UserId = id, Email = request.Email.Trim().ToLowerInvariant(), Role = AuthRoles.Admin });
    }

    [Authorize(Roles = "Admin,Admin.Admins")]
    [HttpPut("admins/{adminUserId:long}")]
    public async Task<ActionResult<AuthResponse>> UpdateAdmin([FromRoute] long adminUserId, [FromBody] UpdateAdminRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse { Success = false, Message = "Invalid admin request." });
        }

        var updated = await _authRepository.UpdateAdminAsync(adminUserId, request, cancellationToken);
        return updated
            ? Ok(new AuthResponse { Success = true, Message = "Admin updated.", UserId = adminUserId })
            : NotFound(new AuthResponse { Success = false, Message = "Admin not found." });
    }

    [Authorize(Roles = "Admin,Admin.Admins")]
    [HttpPut("admins/{adminUserId:long}/password")]
    public async Task<ActionResult<AuthResponse>> ResetAdminPassword([FromRoute] long adminUserId, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse { Success = false, Message = "Invalid password request." });
        }

        var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == adminUserId)
        {
            return BadRequest(new AuthResponse { Success = false, Message = "You cannot reset your own password from this screen." });
        }

        var admins = await _authRepository.GetAdminsAsync(cancellationToken);
        var admin = admins.FirstOrDefault(a => a.Id == adminUserId);
        if (admin is null)
        {
            return NotFound(new AuthResponse { Success = false, Message = "Admin not found." });
        }

        var passwordHash = _passwordHasher.HashPassword(admin.Email, request.NewPassword);
        var updated = await _authRepository.UpdateUserPasswordHashAsync(adminUserId, passwordHash, cancellationToken);
        return updated
            ? Ok(new AuthResponse { Success = true, Message = "Password updated.", UserId = adminUserId })
            : NotFound(new AuthResponse { Success = false, Message = "Admin not found." });
    }

    [Authorize(Roles = "Admin,Admin.Admins")]
    [HttpDelete("admins/{adminUserId:long}")]
    public async Task<ActionResult<AuthResponse>> DeleteAdmin([FromRoute] long adminUserId, CancellationToken cancellationToken)
    {
        var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == adminUserId)
        {
            return BadRequest(new AuthResponse { Success = false, Message = "You cannot delete your own admin account." });
        }

        var admins = await _authRepository.GetAdminsAsync(cancellationToken);
        if (admins.All(a => a.Id != adminUserId))
        {
            return NotFound(new AuthResponse { Success = false, Message = "Admin not found." });
        }

        var deleted = await _authRepository.DeleteUserAsync(adminUserId, cancellationToken);
        return deleted
            ? Ok(new AuthResponse { Success = true, Message = "Admin deleted.", UserId = adminUserId })
            : NotFound(new AuthResponse { Success = false, Message = "Admin not found." });
    }

    [Authorize(Roles = "Admin,Admin.Customers")]
    [HttpPut("users/{userId:long}/active/{isActive:bool}")]
    public async Task<ActionResult> SetUserActive([FromRoute] long userId, [FromRoute] bool isActive, CancellationToken cancellationToken)
    {
        var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!isActive && long.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == userId)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "You cannot deactivate your own admin account."
            });
        }

        var updated = await _authRepository.SetUserActiveAsync(userId, isActive, cancellationToken);
        if (!updated)
        {
            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "User not found."
            });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            Message = isActive ? "User activated successfully." : "User deactivated successfully.",
            UserId = userId
        });
    }

    [Authorize(Roles = "Admin,Admin.Notifications")]
    [HttpGet("follow-up-leads")]
    public async Task<ActionResult<IReadOnlyList<AdminLeadNotificationDto>>> GetFollowUpLeads(CancellationToken cancellationToken)
    {
        var leads = await _authRepository.GetFollowUpCandidatesAsync(cancellationToken);
        return Ok(leads);
    }

    [Authorize(Roles = "Admin,Admin.Notifications")]
    [HttpPost("follow-up-leads/{userId:long}/email")]
    public async Task<ActionResult<AuthResponse>> SendFollowUpLeadEmail([FromRoute] long userId, [FromBody] SendLeadFollowUpEmailRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Subject and message are required."
            });
        }

        var lead = await _authRepository.GetFollowUpCandidateAsync(userId, cancellationToken);
        if (lead is null)
        {
            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "This user is no longer available for follow-up."
            });
        }

        var (success, message) = await _adminEmailDeliveryService.SendPlainTextEmailAsync(lead.Email, request.Subject, request.Message, cancellationToken);
        if (!success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new AuthResponse
            {
                Success = false,
                Message = message,
                Email = lead.Email,
                UserId = lead.UserId
            });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            Message = $"Follow-up email sent to {lead.FullName}.",
            Email = lead.Email,
            UserId = lead.UserId
        });
    }

    private string CreateToken(UserAccount user, DateTime expiresAtUtc)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var roleValue = string.IsNullOrWhiteSpace(user.Role) ? AuthRoles.User : user.Role.Trim();
        var roleClaims = roleValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (string.Equals(roleValue, AuthRoles.User, StringComparison.OrdinalIgnoreCase))
        {
            roleClaims = new List<string> { AuthRoles.User };
        }
        else if (!roleClaims.Any(r => string.Equals(r, AuthRoles.Admin, StringComparison.OrdinalIgnoreCase)))
        {
            roleClaims.Insert(0, AuthRoles.Admin);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        foreach (var roleClaim in roleClaims)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleClaim));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    private int GetExpiryMinutes()
    {
        var configured = _configuration["Jwt:ExpiryMinutes"];
        return int.TryParse(configured, out var minutes) && minutes > 0 ? minutes : 120;
    }
}
