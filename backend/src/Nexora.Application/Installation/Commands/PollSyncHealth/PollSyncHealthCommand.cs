using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Installation.Commands.PollSyncHealth;

/// <summary>
/// Disparado periodicamente pelo <c>SyncOutboxWorker</c> (BackgroundService, ADR-037) — porta de
/// <c>sync-worker.ts</c>. Sem parâmetros: sempre atua sobre a única instalação edge local
/// (uma loja = um edge, ADR-004). O resultado do ping fica gravado em
/// <c>EdgeInstallation.Health</c>/<c>LastSeenAt</c> para o probe de saúde (GetInstallationHealthQuery)
/// responder sem precisar de uma chamada de rede síncrona a cada requisição.
/// </summary>
public sealed record PollSyncHealthCommand : ICommand;
