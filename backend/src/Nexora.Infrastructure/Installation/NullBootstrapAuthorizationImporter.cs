using Nexora.Application.Installation.Abstractions;
using Microsoft.Extensions.Logging;

namespace Nexora.Infrastructure.Installation;

/// <summary>Mesma nota de escopo de <see cref="NullBootstrapCatalogImporter"/>, para papéis/usuários/vínculos (módulo de Autenticação).</summary>
public sealed class NullBootstrapAuthorizationImporter : IBootstrapAuthorizationImporter
{
    private readonly ILogger<NullBootstrapAuthorizationImporter> _logger;

    public NullBootstrapAuthorizationImporter(ILogger<NullBootstrapAuthorizationImporter> logger)
    {
        _logger = logger;
    }

    public Task ImportAsync(Guid tenantId, Guid storeId, string authorizationJson, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "bootstrap.authorization.skipped: implementação real pendente (módulo de Autenticação). TenantId={TenantId}, StoreId={StoreId}, Bytes={Bytes}",
            tenantId, storeId, authorizationJson.Length);

        return Task.CompletedTask;
    }
}
