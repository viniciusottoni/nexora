using System.Security.Claims;
using Nexora.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Nexora.Api.Edge.Infrastructure.Auth;

/// <summary>
/// Contexto de tenant do edge — instalação fixa por loja (ADR-004: "uma loja = um tenant" no
/// edge), configurada por <see cref="EdgeInstallationOptions"/> (porta de EDGE_TENANT_ID —
/// auth.module.ts/request-context.middleware.ts). <see cref="TenantId"/> nunca é nulo, mesmo em
/// endpoints públicos (login por PIN) — diferente do cloud. Usuário/papéis/permissões/dispositivo/
/// sessão vêm das claims do JWT quando a requisição está autenticada.
/// </summary>
/// <remarks>
/// Vive em <c>Api.Edge</c>, não em <c>Infrastructure</c>: depende de <c>IHttpContextAccessor</c>
/// (ASP.NET Core), e a fronteira de camadas (ADR-039, ver CLAUDE.md) proíbe
/// <c>Nexora.Infrastructure</c> de referenciar ASP.NET Core além de abstrações de
/// <c>Options</c>. Só <c>Api.Edge</c>/<c>Api.Cloud</c> podem referenciar tudo.
/// </remarks>
public sealed class EdgeCurrentTenantContext : ICurrentTenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EdgeInstallationOptions _installation;

    public EdgeCurrentTenantContext(IHttpContextAccessor httpContextAccessor, IOptions<EdgeInstallationOptions> installation)
    {
        _httpContextAccessor = httpContextAccessor;
        _installation = installation.Value;
    }

    public Guid? TenantId => _installation.TenantId;

    public Guid? StoreId => ReadGuidClaim("sid") ?? _installation.StoreId;

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

    private IReadOnlyList<string> ReadListClaim(string type) =>
        User?.FindAll(type).Select(c => c.Value).ToArray() ?? Array.Empty<string>();
}
