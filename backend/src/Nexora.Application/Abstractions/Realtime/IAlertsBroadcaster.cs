namespace Nexora.Application.Abstractions.Realtime;

/// <summary>
/// Porta de alerta dirigido a um PAPEL ou a uma PESSOA específica (US-025 "Chamar garçom pela
/// mesa", US-026 "Solicitar a conta", ADR-011) — diferente de <see cref="ITableMapBroadcaster"/>
/// (atualiza o SNAPSHOT do mapa de mesas, sala por tenant, todo mundo que olha o mapa recebe) e de
/// <see cref="IOrderConsumptionBroadcaster"/> (sala por MESA, quem está naquela mesa recebe): este
/// broadcaster entrega só a quem PRECISA agir — o garçom responsável (ou todos os garçons, se ainda
/// não há responsável ou depois do escalonamento) e o caixa (US-026 §4: "o caixa e o garçom
/// responsável devem ser alertados"). US-025 §10: "no celular do garçom, vibração e som curto — o
/// alerta precisa vencer o ruído do salão" é o motivo de existir uma sala PRÓPRIA por pessoa/papel
/// em vez de reaproveitar o grupo de tenant do mapa (que dispararia o alerta em TODO dispositivo
/// conectado ao mapa, não só no do responsável).
///
/// Implementada com SignalR em <c>Nexora.Api.Edge</c> (<c>Realtime.SignalRAlertsBroadcaster</c> +
/// <c>Hubs.AlertsHub</c>) — <c>Application</c> nunca referencia SignalR (ADR-039).
/// </summary>
public interface IAlertsBroadcaster
{
    /// <summary>
    /// EVT-021 <c>table.waiter_called</c> — sala <c>user:{waiterId}</c> quando a sessão já tem
    /// responsável definido; <c>role:waiter</c> (todos os garçons do ambiente) quando
    /// <paramref name="waiterId"/> é nulo (US-025 §3.1, sessão aberta por QR ainda sem garçom
    /// atribuído — ver docstring de <c>WaiterCallCoordinator</c> para a decisão completa).
    /// </summary>
    Task WaiterCalled(Guid tenantId, Guid tableId, string tableLabel, Guid? waiterId, CancellationToken cancellationToken);

    /// <summary>
    /// EVT-022 <c>table.bill_requested</c> — sempre <c>role:cashier</c> E <c>user:{waiterId}</c>
    /// quando há responsável (RN-003: "cada transição gera alerta aos perfis envolvidos", os dois
    /// ao mesmo tempo, US-026 §4).
    /// </summary>
    Task BillRequested(
        Guid tenantId, Guid tableId, string tableLabel, string splitMode, short? people, Guid? waiterId, CancellationToken cancellationToken);

    /// <summary>
    /// US-026 §4, cenário "Novo pedido após solicitar a conta": "o caixa deve ser informado da
    /// mudança" quando um item novo devolve a sessão de <c>BILL_REQUESTED</c> para <c>OPEN</c>.
    /// </summary>
    Task BillRequestCancelled(Guid tenantId, Guid tableId, string tableLabel, CancellationToken cancellationToken);

    /// <summary>
    /// US-025 §4, cenário "Escalonamento por falta de atendimento" — chamado pelo
    /// <c>WaiterCallEscalationWorker</c> quando o limiar configurado é ultrapassado; escala para
    /// <c>role:waiter</c> inteiro (todos os garçons do ambiente), não só o responsável original.
    /// </summary>
    Task WaiterCallEscalated(Guid tenantId, Guid tableId, string tableLabel, CancellationToken cancellationToken);

    /// <summary>
    /// E-08/US-081 §7 <c>{ type: "alert.raised", data: {...} }</c> — canal genérico do motor de
    /// alertas (US-080/US-082), diferente dos métodos acima (específicos de US-025/US-026): entrega
    /// a <c>alert.TargetRoles</c> (<c>role:{papel}</c>, um por papel) e/ou <c>alert.TargetUserId</c>
    /// (<c>user:{id}</c>) — as MESMAS salas que o <c>AlertsHub</c> já usa (<c>RoleGroup</c>/
    /// <c>UserGroup</c>), só que resolvidas dinamicamente a partir da matriz de direcionamento
    /// (US-082), não fixas por método.
    /// </summary>
    Task AlertRaised(Domain.Metrics.Alert alert, CancellationToken cancellationToken);

    /// <summary>
    /// US-083 §10 "som toca apenas na criação do grupo, nunca nas atualizações" — entregue quando
    /// um novo alerta se junta a um grupo já aberto (mesmo <c>GroupKey</c>): o cliente atualiza a
    /// contagem sem repetir som/vibração.
    /// </summary>
    Task AlertGroupUpdated(Domain.Metrics.Alert alert, int groupCount, CancellationToken cancellationToken);

    /// <summary>US-080 §4 "Resolução automática" — avisa quem já recebeu o alerta que ele foi encerrado (sai da lista de pendentes).</summary>
    Task AlertResolved(Domain.Metrics.Alert alert, CancellationToken cancellationToken);
}
