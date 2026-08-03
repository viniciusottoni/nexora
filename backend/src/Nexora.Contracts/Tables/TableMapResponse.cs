namespace Nexora.Contracts.Tables;

/// <summary>Porta de <c>tableMapResponseSchema</c> (<c>packages/contracts/src/tables.ts</c>) — US-023 §7.</summary>
public sealed record TableMapResponse(IReadOnlyList<TableMapEntryResponse> Tables);

/// <summary>
/// Uma mesa no mapa do salão. <see cref="Area"/> é o NOME do ambiente (ex. "Salão"), não seu id —
/// o agrupamento visual por ambiente é responsabilidade do frontend (US-023 §10), a API só
/// entrega a lista plana já com o rótulo do ambiente em cada item (contrato §7 do documento não
/// aninha por ambiente).
/// </summary>
public sealed record TableMapEntryResponse(
    Guid Id,
    string Label,
    string Area,
    string Status,
    short Seats,
    TableMapSessionResponse? Session,
    TableMapFlagsResponse Flags);

/// <param name="SessionId">
/// US-025/US-026: o garçom precisa do id da sessão para confirmar atendimento
/// (<c>POST /v1/tables/{id}/acknowledge-call</c> já usa o id da MESA) e para pedir a conta direto
/// do mapa (<c>POST /v1/sessions/{id}/request-bill</c>, que exige o id da SESSÃO — o mapa não
/// expunha esse campo antes destas duas histórias, porque nenhuma ação de escrita partia do mapa
/// até agora).
/// </param>
public sealed record TableMapSessionResponse(
    DateTimeOffset OpenedAt,
    int MinutesOpen,
    decimal Total,
    short GuestCount,
    TableMapWaiterResponse? Waiter,
    Guid SessionId);

public sealed record TableMapWaiterResponse(Guid Id, string Name);

/// <summary>
/// <see cref="WaiterCalled"/> (US-025) reflete um <c>Alert</c> tipo <c>WAITER_CALLED</c> ainda não
/// resolvido para a sessão corrente da mesa (ver <c>GetTableMapQueryHandler.BuildWaiterCalledSessionIdsAsync</c>)
/// — some assim que o garçom confirma o atendimento. <see cref="BillRequested"/> (US-026) deriva de
/// <c>TableSessionStatus.BillRequested</c>/<c>bill_requested_at</c>, sem precisar de tabela extra.
/// </summary>
public sealed record TableMapFlagsResponse(
    bool WaiterCalled,
    bool BillRequested,
    int ItemsReadyToServe,
    bool AboveAvgDuration);
