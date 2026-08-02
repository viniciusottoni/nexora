using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Awaken.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Awaken.Infrastructure.Services;

public class CurrentAdminService(IHttpContextAccessor httpContextAccessor) : ICurrentAdminService
{
    public Guid AdminUserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)
                       ?? httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true
        && httpContextAccessor.HttpContext.User.IsInRole("AdminSite");
}
