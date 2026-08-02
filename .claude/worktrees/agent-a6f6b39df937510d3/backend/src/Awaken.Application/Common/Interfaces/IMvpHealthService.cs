using Awaken.Contracts.Admin.MvpHealth;

namespace Awaken.Application.Common.Interfaces;

/// <summary>
/// US-216: agrega sinais de saúde de todos os domínios operacionais do MVP em uma visão consolidada.
/// RN-003: nunca reporta "healthy" quando não há dado real disponível — usa "no_data".
/// ADR-015: nunca expõe credenciais, tokens ou payloads sensíveis.
/// </summary>
public interface IMvpHealthService
{
    /// <summary>Retorna o status agregado de saúde de todos os domínios operacionais do MVP.</summary>
    Task<MvpHealthStatusResponse> GetMvpHealthAsync(CancellationToken cancellationToken = default);
}
