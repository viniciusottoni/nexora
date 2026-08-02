using Nexora.Application.Installation.Abstractions;
using Microsoft.Extensions.Logging;

namespace Nexora.Infrastructure.Installation;

/// <summary>
/// Implementação provisória de <see cref="IBootstrapCatalogImporter"/>: só registra em log que
/// o bloco de catálogo chegou e quantos itens tinha por chave, sem persistir nada. Mantém
/// <c>ImportBootstrapCommand</c> funcional de ponta a ponta (identidade/config do tenant já
/// gravam de verdade) enquanto o módulo de Catálogo — fora do escopo desta portabilidade — não
/// tiver uma variante de <c>Create</c> com id explícito para os agregados de
/// <c>Nexora.Domain.Catalog</c> (ver nota de escopo em <see cref="IBootstrapCatalogImporter"/>).
/// Troque o registro de DI por uma implementação real assim que esse módulo existir.
/// </summary>
public sealed class NullBootstrapCatalogImporter : IBootstrapCatalogImporter
{
    private readonly ILogger<NullBootstrapCatalogImporter> _logger;

    public NullBootstrapCatalogImporter(ILogger<NullBootstrapCatalogImporter> logger)
    {
        _logger = logger;
    }

    public Task ImportAsync(Guid tenantId, Guid storeId, string catalogJson, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "bootstrap.catalog.skipped: implementação real pendente (módulo de Catálogo). TenantId={TenantId}, StoreId={StoreId}, Bytes={Bytes}",
            tenantId, storeId, catalogJson.Length);

        return Task.CompletedTask;
    }
}
