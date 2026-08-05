using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Support;

/// <summary>
/// Apuração do valor esperado em caixa (US-055 §4/§7, cenário "Composição do valor esperado";
/// <c>docs/domain/04-Caixa-e-Pagamento.md</c> §"Apuração do fechamento"): abertura + pagamentos em
/// dinheiro pagos (líquidos de troco) + suprimentos − sangrias. Usado tanto por
/// <c>GetCurrentCashSessionQueryHandler</c> (leitura, nunca persiste) quanto por
/// <c>CloseCashSessionCommandHandler</c> (persiste via <see cref="CashSession.SetExpectedAmount"/>
/// antes de <see cref="CashSession.Close"/>). <see cref="Calculate"/> é a função pura (testada
/// isoladamente por <c>CashExpectedAmountCalculatorTests</c>, sem banco); <see cref="CalculateAsync"/>
/// só busca as linhas e delega a soma.
/// </summary>
public static class CashExpectedAmountCalculator
{
    public static async Task<CashExpectedAmountResponse> CalculateAsync(
        IApplicationDbContext db, Guid cashSessionId, decimal openingAmount, CancellationToken cancellationToken)
    {
        var cashPayments = await db.Payments.AsNoTracking()
            .Where(p => p.CashSessionId == cashSessionId && p.Method == PaymentMethod.Cash && p.Status == PaymentStatus.Paid)
            .Select(p => new CashPaymentAmounts(p.Amount, p.ChangeAmount))
            .ToListAsync(cancellationToken);

        var movements = await db.CashMovements.AsNoTracking()
            .Where(m => m.CashSessionId == cashSessionId)
            .Select(m => new CashMovementAmount(m.Type, m.Amount))
            .ToListAsync(cancellationToken);

        return Calculate(openingAmount, cashPayments, movements);
    }

    /// <summary>Função pura — sem I/O, testável sem banco. Ver docstring da classe.</summary>
    public static CashExpectedAmountResponse Calculate(
        decimal openingAmount,
        IReadOnlyCollection<CashPaymentAmounts> cashPayments,
        IReadOnlyCollection<CashMovementAmount> movements)
    {
        // Líquido de troco (documento 04: "− SUM(change_amount)") — dinheiro que efetivamente fica
        // na gaveta, não o valor bruto recebido do cliente.
        var netCashPayments = cashPayments.Sum(p => p.Amount) - cashPayments.Sum(p => p.ChangeAmount);

        var supplies = movements.Where(m => m.Type == CashMovementType.Supply).Sum(m => m.Amount);
        // Sinal negativo já embutido no valor (US-055 §7: "withdrawals": -15000) — Total soma direto.
        var withdrawals = -movements.Where(m => m.Type == CashMovementType.Withdrawal).Sum(m => m.Amount);

        var total = openingAmount + netCashPayments + supplies + withdrawals;

        return new CashExpectedAmountResponse(openingAmount, netCashPayments, supplies, withdrawals, total);
    }
}

/// <summary>Projeção mínima de <c>Payment</c> (dinheiro pago) usada pela apuração — evita que a função pura dependa da entidade EF inteira.</summary>
public sealed record CashPaymentAmounts(decimal Amount, decimal ChangeAmount);

/// <summary>Projeção mínima de <c>CashMovement</c> usada pela apuração.</summary>
public sealed record CashMovementAmount(CashMovementType Type, decimal Amount);
