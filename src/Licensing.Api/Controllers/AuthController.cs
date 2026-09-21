using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Licensing.Application.Abstractions;
using Licensing.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Licensing.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ILicensingDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(ILicensingDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.AdminUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive && !u.IsDeleted, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials." });

        var token = GenerateToken(user);
        return Ok(new { accessToken = token, expiresInMinutes = _configuration.GetValue<int>("Jwt:ExpiresMinutes", 60) });
    }

    private string GenerateToken(AdminUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiresMinutes", 60));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            },
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public record LoginRequest(string Username, string Password);
}
