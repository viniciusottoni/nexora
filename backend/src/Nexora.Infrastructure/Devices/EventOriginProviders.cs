using Nexora.Application.Abstractions.Events;

namespace Nexora.Infrastructure.Devices;

/// <summary>
/// Implementações de <see cref="IEventOriginProvider"/> — valor fixo por processo, mesmos
/// valores do <c>EventOrigin</c> TypeScript original ('EDGE'/'CLOUD'). Vivem em
/// <c>Infrastructure/Devices</c> por ora, único consumidor até aqui; são cross-cutting e
/// deveriam se mudar para <c>Infrastructure/Common</c> se outro módulo passar a emitir
/// <c>DomainEvent</c> também.
///
/// Registro pendente (nenhum Program.cs existe ainda neste sandbox):
///   Nexora.Api.Edge  → services.AddSingleton&lt;IEventOriginProvider, EdgeEventOriginProvider&gt;()
///   Nexora.Api.Cloud → services.AddSingleton&lt;IEventOriginProvider, CloudEventOriginProvider&gt;()
/// </summary>
public sealed class EdgeEventOriginProvider : IEventOriginProvider
{
    public string Origin => "EDGE";
}

public sealed class CloudEventOriginProvider : IEventOriginProvider
{
    public string Origin => "CLOUD";
}
