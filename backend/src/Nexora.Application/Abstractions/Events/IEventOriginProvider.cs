namespace Nexora.Application.Abstractions.Events;

/// <summary>
/// Identifica se o processo atual é o servidor edge de uma loja ou a nuvem — usado para
/// preencher <c>DomainEvent.Origin</c> (ADR-006; valores "EDGE"/"CLOUD", os mesmos do
/// <c>EventOrigin</c> do TypeScript original em
/// <c>packages/domain/src/devices/device-registry.ts</c>).
///
/// Abstração nova (não existia no scaffold de Abstractions) porque é a primeira vez que um
/// handler de Application precisa saber em qual dos dois processos está rodando para gravar
/// um evento de domínio — necessidade cross-cutting, não específica de Devices, então
/// qualquer módulo futuro que emita eventos deve reusar esta interface em vez de duplicá-la.
///
/// Implementado em Infrastructure com um valor fixo por processo. Como ainda não existe
/// Program.cs/composição DI em Nexora.Api.Edge nem Nexora.Api.Cloud neste sandbox,
/// o registro (<c>services.AddSingleton&lt;IEventOriginProvider, EdgeEventOriginProvider&gt;()</c>
/// no Edge, <c>CloudEventOriginProvider</c> no Cloud) fica pendente para quando a composição
/// raiz de cada Api for criada.
/// </summary>
public interface IEventOriginProvider
{
    /// <summary>Sempre "EDGE" ou "CLOUD".</summary>
    string Origin { get; }
}
