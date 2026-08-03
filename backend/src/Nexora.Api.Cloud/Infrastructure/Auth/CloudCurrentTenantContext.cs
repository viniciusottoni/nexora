using System.Security.Claims;
using Nexora.Application.Abstractions.Security;

namespace Nexora.Api.Cloud.Infrastructure.Auth;

/// <summary>
/// Contexto de tenant do cloud — resolvido inteiramente das claims do JWT autenticado, nunca do
/// cliente (doc. 05). <see cref="TenantId"/> é nulo em endpoints públicos (login, refresh); nesses
/// dois pontos o RLS nega leitura por padrão (ADR-004, falha fechada) e os handlers de
/// autenticação usam <c>IApplicationDbContext.SetTenantContextAsync</c> para o trecho de fluxo que
/// já conhece o tenant (após resolver o e-mail ou decodificar o refresh token).
/// </summary>
/// <remarks>
/// Vive em <c>Api.Cloud</c>, não em <c>Infrastructure</c> — mesma razão de
/// <c>EdgeCurrentTenantContext</c> (ADR-039: <c>Infrastructure</c> não pode referenciar ASP.NET
/// Core além de abstrações de <c>Options</c>; <c>IHttpContextAccessor</c> só está disponível em
/// <c>Api.Edge</c>/<c>Api.Cloud</c>).
/// </remarks>
public sealed class CloudCurrentTenantContext : ICurrentTenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CloudCurrentTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId => ReadGuidClaim("tid");

    public Guid? StoreId => ReadGuidClaim("sid");

    public Guid? UserId => ReadGuidClaim(ClaimTypes.NameIdentifier) ?? ReadGuidClaim("sub");

    public IReadOnlyList<string> Roles => ReadListClaim("roles");

    public IReadOnlyList<string> Permissions => ReadListClaim("perms");

    public Guid? DeviceId => ReadGuidClaim("did");

    public Guid? SessionId => ReadGuidClaim("ses");

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    private Guid? ReadGuidClaim(string type)
    {
        var value = User?.FindFirst(type)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private string[] ReadListClaim(string type) =>
        User?.FindAll(type).Select(c => c.Value).ToArray() ?? Array.Empty<string>();
}
