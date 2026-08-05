using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;

namespace Nexora.Application.Cashier.Support;

/// <summary>Domínio → contrato de fio (US-055/US-056 §7) — <see cref="CashSessionStatus"/> em maiúsculas, mesma convenção de <c>TableSessionMapper</c>.</summary>
public static class CashSessionMapper
{
    public static CashSessionResponse Map(CashSession session) => new(
        session.Id,
        session.OperatorId,
        session.Status.ToString().ToUpperInvariant(),
        session.OpeningAmount,
        session.OpenedAt,
        session.ClosedAt,
        session.ExpectedAmount,
        session.CountedAmount,
        session.Divergence,
        session.Justification);

    public static CashMovementResponse Map(CashMovement movement) => new(
        movement.Id,
        movement.Type.ToString().ToUpperInvariant(),
        movement.Amount,
        movement.Reason,
        movement.OccurredAt,
        movement.CreatedBy,
        movement.AuthorizedBy);
}
