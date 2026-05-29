using System.Security.Claims;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kohinoorea.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class FirebaseController : ControllerBase
{
    [HttpGet("token")]
    public async Task<ActionResult<string>> GetToken(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (FirebaseAdmin.FirebaseApp.DefaultInstance is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Firebase is not configured on the server.");
        }

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(uid))
        {
            return Unauthorized();
        }

        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "User";
        var normalizedRole = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ? "admin" : "user";

        var additionalClaims = new Dictionary<string, object>
        {
            ["role"] = normalizedRole
        };

        var token = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid, additionalClaims, cancellationToken);
        return Ok(token);
    }
}
