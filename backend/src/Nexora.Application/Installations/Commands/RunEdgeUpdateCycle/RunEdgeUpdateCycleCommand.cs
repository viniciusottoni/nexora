using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Installations.Commands.RunEdgeUpdateCycle;

/// <summary>
/// Disparado periodicamente pelo <c>EdgeUpdateCycleWorker</c> (BackgroundService, ADR-037) — porta
/// do pseudocódigo do US-146 §7 ("verifica versão esperada → verifica pendências de sincronização
/// → gera backup → baixa imagens → aplica migration → health check → ativa ou reverte"). Sem
/// parâmetros: sempre atua sobre a única instalação edge local (uma loja = um edge, ADR-004), mesmo
/// idioma de <c>PollSyncHealthCommand</c>. ADR-019: quem decide agir é o EDGE, puxando — a nuvem
/// nunca dispara isto remotamente.
/// </summary>
public sealed record RunEdgeUpdateCycleCommand : ICommand<RunEdgeUpdateCycleResult>;

/// <summary>
/// <see cref="Status"/> espelha <see cref="Nexora.Domain.Platform.EdgeUpdateStatus"/> quando o
/// ciclo efetivamente tenta atualizar, mais três estados "nada a fazer" que o Domain não precisa
/// conhecer (não são transição de <c>EdgeInstallation</c>): <c>NoUpdatePending</c> (sem
/// <c>TargetVersion</c>), <c>OutsideWindow</c> (fora da janela configurada) e o já existente
/// <c>Deferred</c> (dentro da janela, mas pendências de sincronização acima do limiar).
/// </summary>
public sealed record RunEdgeUpdateCycleResult(string Status, string? Detail);
