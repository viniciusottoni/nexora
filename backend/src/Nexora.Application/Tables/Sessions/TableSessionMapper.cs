using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;

namespace Nexora.Application.Tables.Sessions;

/// <summary>
/// Monta <see cref="TableSessionResponse"/> a partir do agregado — usado por todos os handlers que
/// devolvem uma sessão (abertura, atualização, consulta, acesso público via QR). <c>CurrentItems</c>
/// sempre vazio e <c>Total</c> sempre zero nesta wave: lançamento de item é US-030/US-028, fora do
/// escopo de US-021/US-022 — ver docstring de <see cref="TableSessionResponse"/>.
/// </summary>
internal static class TableSessionMapper
{
    public static TableSessionResponse Map(TableSession session, DiningTable table) => new(
        session.Id,
        session.TableId,
        table.Label,
        session.Status.ToString().ToUpperInvariant(),
        session.OpenedAt,
        session.GuestCount,
        session.GuestCountConfirmed,
        session.WaiterId,
        session.OpenedSource,
        Array.Empty<TableSessionItemResponse>(),
        0m,
        session.SplitMode,
        session.SplitPeople);
}
