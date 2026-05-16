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

    public AuthController(IAuthRepository authRepository, IPasswordHasher<string> passwordHasher, IConfiguration configuration)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
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

        var passwordHash = _passwordHasher.HashPassword(request.Email, request.Password);
        var userId = await _authRepository.CreateUserAsync(request, passwordHash, cancellationToken);
        await _authRepository.CreateSignupSubmissionAsync(request, cancellationToken);

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

        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Token is valid.",
            UserId = userId,
            Email = email,
            Role = role
        });
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _authRepository.GetAdminUsersAsync(cancellationToken);
        return Ok(users);
    }

    private string CreateToken(UserAccount user, DateTime expiresAtUtc)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, string.IsNullOrWhiteSpace(user.Role) ? AuthRoles.User : user.Role)
        };

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
