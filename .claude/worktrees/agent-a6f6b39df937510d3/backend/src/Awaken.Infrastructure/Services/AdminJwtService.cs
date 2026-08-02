using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Awaken.Infrastructure.Services;

public class AdminJwtService(IConfiguration config) : IAdminJwtService
{
    public string GenerateToken(AdminUser adminUser)
    {
        var section = config.GetSection("AdminJwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(section.GetValue<int>("ExpiresInMinutes", 480));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, adminUser.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, adminUser.Email),
            new Claim(ClaimTypes.Role, "AdminSite"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
