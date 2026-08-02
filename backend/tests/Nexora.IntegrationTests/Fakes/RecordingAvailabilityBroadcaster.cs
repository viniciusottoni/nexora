using System.Collections.Concurrent;
using Nexora.Application.Abstractions.Realtime;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de teste de <see cref="IAvailabilityBroadcaster"/> — grava cada chamada em vez de falar
/// com um Hub SignalR real (esse é assunto de <c>Nexora.Api.*</c>, fora do que Application/testes de
/// integração de handler devem depender). Usado para provar que
/// <c>MarkProductUnavailableCommandHandler</c>/<c>MarkProductAvailableCommandHandler</c>/
/// <c>RestoreProductsPastBusinessDayCommandHandler</c> chamam o broadcaster de forma SÍNCRONA
/// (aguardada dentro do próprio <c>Handle</c>) — ao terminar <c>await sender.Send(...)</c>, a lista
/// já contém a chamada, sem precisar de nenhum <c>Task.Delay</c>/polling no teste (US-015: "não dá
/// para testar 2 segundos de forma determinística, mas dá para provar que não foi enfileirado para
/// depois").
/// </summary>
public sealed class RecordingAvailabilityBroadcaster : IAvailabilityBroadcaster
{
    public sealed record UnavailableCall(Guid TenantId, Guid ProductId, string Reason, DateTimeOffset UnavailableSince);

    public sealed record AvailableCall(Guid TenantId, Guid ProductId);

    public ConcurrentQueue<UnavailableCall> UnavailableCalls { get; } = new();

    public ConcurrentQueue<AvailableCall> AvailableCalls { get; } = new();

    public Task ProductMarkedUnavailableAsync(
        Guid tenantId, Guid productId, string reason, DateTimeOffset unavailableSince, CancellationToken cancellationToken)
    {
        UnavailableCalls.Enqueue(new UnavailableCall(tenantId, productId, reason, unavailableSince));
        return Task.CompletedTask;
    }

    public Task ProductMarkedAvailableAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        AvailableCalls.Enqueue(new AvailableCall(tenantId, productId));
        return Task.CompletedTask;
    }
}
