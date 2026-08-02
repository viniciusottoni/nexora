using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Awaken.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            if (TryGetUserId(out var id)) return id;
            throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");
        }
    }

    public bool TryGetUserId(out Guid userId)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var claim = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user?.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
