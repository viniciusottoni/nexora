namespace Awaken.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string[] roles);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
    Guid? ValidateAccessToken(string token);
}
