namespace Nexora.Contracts.Cashier;

/// <summary>
/// Porta de <c>GET /v1/cash/open-sessions</c> (US-050 §7) — visão do CAIXA sobre as sessões de
/// mesa abertas: densidade máxima, prioridade a conta solicitada, busca por mesa/comanda,
/// totalizador do salão. Segunda "visão" dos mesmos dados de
/// <see cref="Nexora.Contracts.Tables.TableMapResponse"/> (US-023), que é a visão do GARÇOM
/// (agrupada por ambiente, com sinais de atendimento) — as duas
/// não compartilham DTO de propósito: cada persona lê só os campos que sua tela precisa.
/// <see cref="OpenSessionEntryResponse.Total"/> é <c>decimal</c> (serializado como string pelo
/// <c>MoneyJsonConverter</c>, ADR-017) — a spec da US mostra o exemplo em centavos inteiros
/// (formato de um rascunho anterior do contrato), mas a convenção real do código é decimal/string
/// em toda parte, seguida aqui.
/// </summary>
public sealed record OpenSessionsResponse(IReadOnlyList<OpenSessionEntryResponse> Sessions, OpenSessionsSummaryResponse Summary);

/// <param name="Table">Rótulo da mesa (<c>dining_table.label</c>) — não o id, o caixa lê o número impresso na mesa.</param>
/// <param name="Area">Nome do ambiente (ex. "Salão"), mesma convenção de <c>TableMapEntryResponse.Area</c>.</param>
/// <param name="Total">Soma dos itens NÃO cancelados de todos os pedidos da sessão, em tempo real — nunca <c>table_session.total_amount</c> (só preenchido no pagamento, ver <c>TableSession.MarkAsPaid</c>), mesmo raciocínio do §12 de US-023.</param>
/// <param name="Status"><c>OPEN</c> | <c>BILL_REQUESTED</c> | <c>PAID</c> | <c>CLOSED</c> (este último só por inconsistência transitória — sessão fechada normalmente já foi liberada e não aparece aqui).</param>
/// <param name="WaitingSeconds">Segundos desde que a conta foi pedida — só preenchido quando <see cref="Status"/> é <c>BILL_REQUESTED</c> (US-050 §7: "só quando aplicável").</param>
/// <param name="PendingItems">Itens ainda não SERVIDOS (produção ou prontos aguardando entrega) — mesma definição de "pendente" usada em <c>PendingItemsClosePolicy</c> (US-035), para o caixa decidir se é seguro fechar.</param>
/// <param name="OrderCode">
/// [DECISÃO] "Comanda" não tem um código de apresentação próprio no modelo de dados — quem tem
/// <c>short_code</c> (ADR-016, ex. "A47") é o PEDIDO (<see cref="Nexora.Domain.Operation.Order"/>), e uma
/// sessão de mesa pode ter mais de um pedido ao longo da visita. Aqui expomos o <c>short_code</c>
/// do pedido mais recente da sessão — o que está atualmente na tela do KDS/impresso para o
/// cliente — como o identificador de "comanda" que a busca do caixa usa (ver
/// <c>GetOpenSessionsQueryHandler.MatchesSearch</c>). Nulo quando a sessão ainda não tem nenhum
/// pedido lançado.
/// </param>
public sealed record OpenSessionEntryResponse(
    Guid SessionId,
    string Table,
    string Area,
    DateTimeOffset OpenedAt,
    int MinutesOpen,
    short GuestCount,
    OpenSessionWaiterResponse? Waiter,
    decimal Total,
    string Status,
    DateTimeOffset? BillRequestedAt,
    int? WaitingSeconds,
    int PendingItems,
    string? OrderCode);

public sealed record OpenSessionWaiterResponse(Guid Id, string Name);

/// <param name="OpenSessions">Contagem de sessões abertas no salão — Gherkin "Visão de todas as comandas".</param>
/// <param name="TotalOpen">
/// Soma do <see cref="OpenSessionEntryResponse.Total"/> de TODAS as sessões abertas do salão —
/// sempre sobre o conjunto INTEIRO, não sobre o resultado filtrado por <c>q</c> (busca): é um
/// indicador do salão como um todo ("quanto está em aberto agora"), não do resultado de uma busca
/// pontual por uma mesa específica.
/// </param>
public sealed record OpenSessionsSummaryResponse(int OpenSessions, decimal TotalOpen);
