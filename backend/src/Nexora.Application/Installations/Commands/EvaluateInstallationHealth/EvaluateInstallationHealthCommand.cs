using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Installations.Commands.EvaluateInstallationHealth;

/// <summary>
/// Avalia a saúde de UMA instalação edge (US-140 §9 "a detecção é da nuvem, não do edge") —
/// disparado por <c>InstallationHealthEvaluationWorker</c>, um comando por instalação, exatamente
/// como <c>AlertEvaluationWorker</c> despacha <c>EvaluateCloudAlertConditionsCommand</c> um por
/// tenant. Mantém o worker fino e a lógica de transição de estado (abrir/fechar
/// <c>InstallationIncident</c>, emitir <c>DomainEvent</c>, notificar a plataforma) testável via
/// integração sem precisar de um <c>BackgroundService</c> rodando.
/// </summary>
public sealed record EvaluateInstallationHealthCommand(Guid TenantId, Guid InstallationId) : ICommand<EvaluateInstallationHealthResult>;

/// <summary>Resultado da avaliação — <see cref="Health"/> é o rótulo de fio ("OK"/"DEGRADED"/"DOWN").</summary>
public sealed record EvaluateInstallationHealthResult(string Health, bool TransitionOccurred);
