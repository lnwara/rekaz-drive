using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace RekazDrive.WebApi.Controllers;

[ApiController]
[Route("v1/auth")] 
public sealed class AuthController : ControllerBase
{
    public sealed record LoginRequest(string Username, string Password);

    private readonly IConfiguration _config;
    public AuthController(IConfiguration config) => _config = config;

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var expectedUser = _config["Auth:Username"] ?? "admin";
        var expectedPass = _config["Auth:Password"] ?? "admin";
        if (!string.Equals(request.Username, expectedUser, StringComparison.Ordinal) ||
            !string.Equals(request.Password, expectedPass, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var issuer = _config["Jwt:Issuer"] ?? "rekazdrive";
        var audience = _config["Jwt:Audience"] ?? "rekazdrive-clients";
        var signingKey = _config["Jwt:SigningKey"] ?? "SigningKey"; // 32+ chars
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddHours(12);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, request.Username)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.WriteToken(token);
        return Ok(new
        {
            access_token = jwt,
            token_type = "Bearer",
            expires_in = (int)(expires - now).TotalSeconds
        });
    }
}

