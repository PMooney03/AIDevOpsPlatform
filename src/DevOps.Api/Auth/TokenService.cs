using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevOps.Application.Common;
using DevOps.Application.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevOps.Api.Auth;

public sealed class TokenService(IOptions<AuthOptions> options)
{
    public LoginResponse Login(LoginRequest request)
    {
        var settings = options.Value;
        var username = request.Username?.Trim() ?? string.Empty;
        var user = settings.Users.FirstOrDefault(item =>
            string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase));

        if (user is null || user.Password != request.Password)
        {
            throw new DomainValidationException("credentials", "Username or password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(settings.SigningKey) || settings.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Auth:SigningKey must be at least 32 characters.");
        }

        var role = string.IsNullOrWhiteSpace(user.Role) ? PlatformRoles.Viewer : user.Role.Trim();
        var expires = DateTimeOffset.UtcNow.Add(settings.TokenLifetime);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username, role, expires);
    }
}
