using Awaken.Contracts.Admin.Readiness;

namespace Awaken.Application.Common.Interfaces;

/// <summary>
/// US-218: avalia o readiness de configuração obrigatória, build mobile e CI por ambiente.
/// RN-001: implementações NUNCA devem retornar valores sensíveis reais — apenas presença/forma/status.
/// </summary>
public interface IReadinessCheckService
{
    /// <summary>Retorna o readiness agregado de todos os ambientes monitorados (dev, staging, produção).</summary>
    Task<ReadinessStatusResponse> GetReadinessStatusAsync(CancellationToken cancellationToken = default);
}
