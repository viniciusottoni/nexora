using Nexora.Application.Installation.Abstractions;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de teste de <see cref="ISyncHealthPoller"/> (US-034) — devolve um resultado controlado
/// pelo teste a cada chamada, sem depender de rede real (o real <c>SyncHealthPoller</c> faz HTTP
/// contra <c>EdgeInstallationIdentityOptions.SyncEndpoint</c>). O teste seta
/// <see cref="Next"/> antes de cada <c>sender.Send(new PollSyncHealthCommand())</c> para simular a
/// sequência "nuvem OK -> cai -> volta" do cenário Gherkin "Retorno da conexão" (US-034 §4).
/// </summary>
public sealed class SequencedSyncHealthPoller : ISyncHealthPoller
{
    public SyncHealthPollResult Next { get; set; } = new(DependencyStatus.Ok, 200);

    public Task<SyncHealthPollResult> PollAsync(CancellationToken cancellationToken) => Task.FromResult(Next);
}
