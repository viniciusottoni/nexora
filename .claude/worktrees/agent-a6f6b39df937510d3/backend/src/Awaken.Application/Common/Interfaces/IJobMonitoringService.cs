using Awaken.Contracts.Admin.Routines;

namespace Awaken.Application.Common.Interfaces;

/// <summary>
/// US-221: introspecção somente leitura do sistema de jobs (Hangfire) para o admin site —
/// workers ativos, rotinas recorrentes, filas e execuções recentes.
/// RN-005: a implementação NUNCA deve expor uma operação que dispare/cancele/reagende um job —
/// apenas leitura de status.
/// </summary>
public interface IJobMonitoringService
{
    /// <summary>Retorna a visão operacional agregada de workers, rotinas, filas e execuções recentes.</summary>
    Task<RoutinesOverviewResponse> GetRoutinesOverviewAsync(CancellationToken cancellationToken = default);
}
