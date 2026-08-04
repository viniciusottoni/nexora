using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Alerts.Commands.EvaluateCloudAlertConditions;

/// <summary>
/// US-080 §9 "alertas de gestão que dependem de consolidação... rodam na nuvem" — avalia
/// <c>CASH_DIVERGENCE</c> (sessão de caixa fechada acima do limiar) e <c>SYNC_DELAY</c> (instalação
/// edge sem contato recente) para UM tenant. Mesmo padrão de <c>RestoreProductsPastBusinessDayCommand</c>:
/// <c>TenantId</c> explícito, despachado por tenant pelo worker (<c>AlertEvaluationWorker</c>, Cloud).
/// </summary>
public sealed record EvaluateCloudAlertConditionsCommand(Guid TenantId) : ICommand<int>;
