namespace Nexora.Application.Abstractions.Realtime;

/// <summary>
/// Porta de propagação em tempo real da mudança de disponibilidade de um produto (US-015,
/// RF-CAT-07, EVT-051 <c>product.availability_changed</c>) — "a mesa, o garçom, o delivery e o
/// caixa precisam refletir a mudança em até 2 segundos" (US-015 §2/§4). A implementação real usa
/// SignalR (<c>IHubContext&lt;CatalogAvailabilityHub&gt;</c>), mas <c>Application</c> nunca pode
/// referenciar <c>Microsoft.AspNetCore.*</c> (ADR-039, <c>Nexora.ArchitectureTests</c>) — por isso
/// esta abstração existe aqui e é implementada em <c>Nexora.Api.Cloud</c>/<c>Nexora.Api.Edge</c>
/// (o hub em si — <c>CatalogAvailabilityHub</c> — também vive em cada Api, réplica idêntica nos
/// dois processos; ver Program.cs de cada um).
///
/// Chamada SEMPRE de dentro do handler do comando, antes deste retornar (nunca enfileirada para
/// depois) — é o que garante que o broadcast acontece "na mesma requisição" que persistiu a
/// mudança (não dá para testar os "2 segundos" de ponta a ponta de forma determinística num teste
/// automatizado; o que os testes desta história verificam é que a chamada é feita de forma
/// síncrona, sincronicamente aguardada, dentro do mesmo <c>Handle</c> — ver
/// <c>MarkProductUnavailableCommandHandlerTests</c>/<c>AvailabilityIntegrationTests</c>).
///
/// [PENDÊNCIA] Hoje o broadcast só alcança os clientes conectados DIRETAMENTE ao processo que
/// processou o comando (edge OU cloud) — não existe ainda infraestrutura de sincronização de
/// eventos em tempo real entre edge e nuvem (isso é outbox/sync assíncrono, ADR-008 é só sobre
/// saldo de estoque). Propagação completa edge→cloud→outros-edges é escopo de uma US de
/// sincronização futura, fora desta história (US-015 §9: "o cardápio de delivery, que roda na
/// nuvem, só reflete após a sincronização — limitação real que precisa ser comunicada ao gestor").
/// </summary>
public interface IAvailabilityBroadcaster
{
    /// <summary>Produto marcado indisponível — broadcast <c>product.unavailable</c> (US-015 §7) aos clientes conectados a este processo.</summary>
    Task ProductMarkedUnavailableAsync(
        Guid tenantId,
        Guid productId,
        string reason,
        DateTimeOffset unavailableSince,
        CancellationToken cancellationToken);

    /// <summary>Produto de volta à disponibilidade (manual ou automático no início do dia operacional) — broadcast <c>product.available</c>.</summary>
    Task ProductMarkedAvailableAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);
}
