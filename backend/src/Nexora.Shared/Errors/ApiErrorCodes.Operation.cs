namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro do módulo de operação — ambientes e mesas do salão (US-020, ADR-021).</summary>
public static partial class ApiErrorCodes
{
    public const string AreaNotFound = "AREA_NOT_FOUND";

    /// <summary>Área com mesa ativa não pode ser excluída — mesma lógica de "recusa exclusão com histórico" aplicada uma camada acima.</summary>
    public const string AreaHasActiveTables = "AREA_HAS_ACTIVE_TABLES";

    public const string TableNotFound = "TABLE_NOT_FOUND";

    /// <summary>Rótulo já usado por outra mesa da mesma loja (<c>uq_table_label</c>, <c>store_id + label</c>).</summary>
    public const string TableLabelAlreadyExists = "TABLE_LABEL_ALREADY_EXISTS";

    /// <summary>
    /// Cenário Gherkin "Exclusão de mesa com histórico": mesa com sessão encerrada não pode ser
    /// excluída fisicamente — a exclusão é recusada e a desativação é oferecida no lugar.
    /// </summary>
    public const string TableHasSessionHistory = "TABLE_HAS_SESSION_HISTORY";

    /// <summary>Exportação de PDF de QR Codes pedida sem nenhuma mesa ativa correspondente ao filtro.</summary>
    public const string TablesExportEmpty = "TABLES_EXPORT_EMPTY";

    /// <summary>
    /// US-022, cenário "Mesa já ocupada": mesa com sessão ativa (<c>uq_session_open</c>) não pode
    /// abrir outra — o chamador deve ser direcionado à sessão existente (<c>meta.sessionId</c>).
    /// </summary>
    public const string TableAlreadyOpen = "TABLE_ALREADY_OPEN";

    /// <summary>
    /// US-021, cenário "Token inválido ou rotacionado": <c>qr_token</c> inexistente ou já
    /// rotacionado — nunca revela nada sobre a existência de outra mesa (RN-015).
    /// </summary>
    public const string InvalidTableToken = "INVALID_TABLE_TOKEN";

    public const string TableSessionNotFound = "TABLE_SESSION_NOT_FOUND";

    /// <summary>PATCH de sessão/reatribuição de garçom recusado por a sessão já não estar aberta.</summary>
    public const string TableSessionNotOpen = "TABLE_SESSION_NOT_OPEN";

    // US-024/US-028 — consumo da mesa em tempo real e repetição de item. `AddOrderItemCommand`
    // é a capacidade MÍNIMA de "lançar item no pedido de uma sessão" construída para preencher a
    // lacuna real de US-030 (Enviar pedido pelo cardápio, E-03, fora deste épico e ainda não
    // implementada) — ver docstring de `Nexora.Application.Orders.Commands.AddOrderItem.AddOrderItemCommandHandler`.

    public const string OrderNotFound = "ORDER_NOT_FOUND";

    public const string OrderItemNotFound = "ORDER_ITEM_NOT_FOUND";

    /// <summary>
    /// US-028, cenário "Item indisponível": produto do item a repetir (ou a lançar) ficou
    /// indisponível — valor literal idêntico ao usado no contrato de API da história
    /// (<c>{ "code": "PRODUCT_UNAVAILABLE" }</c>).
    /// </summary>
    public const string ProductUnavailable = "PRODUCT_UNAVAILABLE";

    /// <summary>Variante sem preço vigente cadastrado no canal de salão (DINE_IN) — não deveria acontecer em operação normal, mas evita lançar um item com preço zerado silenciosamente.</summary>
    public const string OrderItemVariantPriceNotFound = "ORDER_ITEM_VARIANT_PRICE_NOT_FOUND";

    // US-030 (Criar pedido com itens, modificadores e frações) — POST /v1/orders, POST
    // /v1/public/orders e POST /v1/orders/{id}/items.

    /// <summary>
    /// US-030 §4, cenário "Grupo de modificadores obrigatório pendente": um grupo
    /// <c>IsRequired</c> vinculado ao produto do item não teve nenhum modificador escolhido.
    /// Valor literal idêntico ao contrato de API da história (<c>{ "code": "MODIFIER_GROUP_REQUIRED",
    /// "meta": { "itemIndex", "groupId", "groupName" } }</c>).
    /// </summary>
    public const string ModifierGroupRequired = "MODIFIER_GROUP_REQUIRED";

    /// <summary>
    /// Quantidade de modificadores escolhidos de um grupo fora do intervalo <c>MinSelect</c>..<c>MaxSelect</c>
    /// configurado (US-030 §5, RN "grupo respeita mínimo e máximo de seleção") — mesmo formato de
    /// <c>meta</c> de <see cref="ModifierGroupRequired"/>.
    /// </summary>
    public const string ModifierGroupSelectionInvalid = "MODIFIER_GROUP_SELECTION_INVALID";

    /// <summary>
    /// <c>POST /v1/orders/{id}/items</c>: o pedido alvo não está em um status que aceita novo item
    /// (só <c>PLACED</c>/<c>IN_PRODUCTION</c> aceitam, US-030 §4 cenário "Acréscimo a pedido já
    /// confirmado") — rascunho nunca deveria chegar aqui, e pedido pronto/despachado/entregue/
    /// fechado/cancelado não recebe mais item novo.
    /// </summary>
    public const string OrderNotAcceptingItems = "ORDER_NOT_ACCEPTING_ITEMS";

    /// <summary>
    /// US-025, <c>POST /v1/tables/{id}/acknowledge-call</c>: não há nenhum alerta <c>WAITER_CALLED</c>
    /// pendente para a mesa (já foi confirmado antes, ou o cliente nunca chamou).
    /// </summary>
    public const string NoPendingWaiterCall = "NO_PENDING_WAITER_CALL";

    // US-027 (Dividir a conta) — divisão por item, retirada de taxa e pagamento parcial.

    /// <summary>
    /// <c>POST /v1/sessions/{id}/bill/assign-items</c>: pelo menos um item não cancelado da sessão
    /// não foi atribuído a nenhuma pessoa (US-027 §4, cenário "Divisão por item": "nenhum item pode
    /// ficar sem atribuição antes de fechar"). <c>Result.Errors["itemIds"]</c> traz os ids órfãos.
    /// </summary>
    public const string BillItemNotAssigned = "BILL_ITEM_NOT_ASSIGNED";

    /// <summary>
    /// Um item informado numa atribuição (<c>assign-items</c>) não pertence à sessão, já está
    /// cancelado, ou foi atribuído a mais de uma pessoa ao mesmo tempo.
    /// </summary>
    public const string BillItemAssignmentInvalid = "BILL_ITEM_ASSIGNMENT_INVALID";

    /// <summary>
    /// <c>GET /v1/sessions/{id}/bill?split=BY_AMOUNT</c> ou o registro do pagamento parcial:
    /// <c>amount</c> ausente, menor ou igual a zero, ou maior que o saldo em aberto da conta.
    /// </summary>
    public const string BillInvalidAmount = "BILL_INVALID_AMOUNT";

    /// <summary>
    /// Registro de pagamento parcial (US-027 §4, cenário "Divisão por valor") pedido para uma
    /// sessão cujo status ainda não é <c>BillRequested</c> — o pagamento parcial só faz sentido
    /// depois que a conta foi pedida.
    /// </summary>
    public const string BillNotRequested = "BILL_NOT_REQUESTED";

    // US-031 (Roteamento simultâneo para cozinha e caixa) — GET /v1/kds/queue reaproveita
    // ApiErrorCodes.StationNotFound (ApiErrorCodes.Stations.cs, já existente).

    // US-035 (Bloquear fechamento com item pendente) — request-bill e pagamento parcial.

    /// <summary>
    /// <c>POST /v1/sessions/{id}/request-bill</c>, <c>POST /v1/public/table/{qrToken}/request-bill</c>
    /// e <c>POST /v1/sessions/{id}/bill/partial-payment</c>: existe item não entregue (status
    /// diferente de SERVED/CANCELLED) e o tenant está no modo <c>BLOCK</c>
    /// (<c>operation.pendingItemsOnClose</c>) sem um <c>X-Authorization-Token</c> válido para a ação
    /// <c>CLOSE_WITH_PENDING</c> — US-035 §7. <c>meta.pendingItems</c> traz <c>{ name, status }</c>
    /// de cada item pendente.
    /// </summary>
    public const string PendingItems = "PENDING_ITEMS";
}
