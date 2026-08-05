using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Cashier.Support;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;
using Nexora.Domain.Finance;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Commands.RegisterPayments;

/// <summary>
/// US-052 (Múltiplas formas de pagamento na mesma conta) e US-058 (Registrar pagamento de
/// maquininha externa) — um único fluxo: cada elemento de <see cref="RegisterPaymentsCommand.Payments"/>
/// pode ou não carregar provedor/NSU/bandeira/parcelas (US-058), mas a validação de soma, a
/// idempotência (<c>Idempotency-Key</c>, tratada pelo middleware, ADR-020) e a transição de estado
/// são as mesmas para os dois.
///
/// A ordem das operações é a que garante a invariante "soma dos pagamentos == total" (ADR-017):
/// primeiro calcula o total EXATO via <see cref="BillQueryCoordinator"/> (a mesma função que
/// <c>GET /v1/sessions/{id}/bill</c> usa — nunca dois cálculos que podem divergir), só then valida a
/// soma informada, só then persiste. Toda a operação falha OU sucede inteira (nenhum pagamento
/// parcial gravado se a soma não bater ou se houver referência duplicada não confirmada).
/// </summary>
internal sealed class RegisterPaymentsCommandHandler : IRequestHandler<RegisterPaymentsCommand, Result<RegisterPaymentsResponse>>
{
    private static readonly string[] ChangeEligibleMethods = { "CASH" };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;

    public RegisterPaymentsCommandHandler(
        IApplicationDbContext db, ICurrentTenantContext tenantContext, IEventOriginProvider eventOrigin)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
    }

    public async Task<Result<RegisterPaymentsResponse>> Handle(RegisterPaymentsCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions.SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result<RegisterPaymentsResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        if (session.Status is TableSessionStatus.Paid or TableSessionStatus.Closed)
        {
            return Result<RegisterPaymentsResponse>.Failure(
                "Esta comanda já foi paga ou encerrada.", ApiErrorCodes.PaymentSessionNotPayable);
        }

        var parsedPayments = new List<(PaymentInput Input, PaymentMethod Method)>(request.Payments.Count);
        foreach (var input in request.Payments)
        {
            if (!TryParseMethod(input.Method, out var method))
            {
                return Result<RegisterPaymentsResponse>.Failure(
                    "Forma de pagamento inválida.", ApiErrorCodes.PaymentMethodInvalid);
            }

            parsedPayments.Add((input, method));
        }

        // Total canônico: a MESMA função que monta a conta em GET /v1/sessions/{id}/bill (US-051),
        // já refletindo desconto de sessão (US-054) e retirada de taxa (US-053) — nunca um segundo
        // cálculo que poderia divergir.
        var billResult = await BillQueryCoordinator.BuildAsync(_db, session, "SINGLE", null, null, null, cancellationToken);
        if (billResult.IsFailure)
        {
            return Result<RegisterPaymentsResponse>.Failure(billResult.Error!, billResult.Code);
        }

        var bill = billResult.Value!;
        var provided = parsedPayments.Sum(p => p.Input.Amount);
        var alreadyPaid = await BillQueryCoordinator.SumPaidAsync(_db, session.Id, cancellationToken);
        var openBalance = bill.Total - alreadyPaid;

        if (openBalance <= 0)
        {
            return Result<RegisterPaymentsResponse>.Failure(
                "Esta comanda já foi paga ou encerrada.", ApiErrorCodes.PaymentSessionNotPayable);
        }

        if (provided != openBalance)
        {
            var difference = openBalance - provided;
            return Result<RegisterPaymentsResponse>.Failure(
                "A soma dos pagamentos não corresponde ao total da conta.",
                ApiErrorCodes.PaymentSumMismatch,
                new Dictionary<string, string[]>
                {
                    ["total"] = new[] { openBalance.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    ["provided"] = new[] { provided.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    ["difference"] = new[] { difference.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                });
        }

        var cashSessionId = await _db.CashSessions.AsNoTracking()
            .Where(cs => cs.StoreId == session.StoreId && cs.OperatorId == _tenantContext.UserId && cs.Status != CashSessionStatus.Closed)
            .Select(cs => (Guid?)cs.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // US-058 §4, cenário "Referência duplicada" — checagem ANTES de criar qualquer pagamento
        // (a operação é tudo-ou-nada): mesmo provider+providerRef já registrado no mesmo turno sem
        // confirmação explícita vira aviso recuperável, não bloqueio silencioso. Se a operação não
        // estiver associada a uma cash_session aberta, loja + dia operacional são o fallback local.
        foreach (var (input, _) in parsedPayments)
        {
            if (string.IsNullOrWhiteSpace(input.Provider) || string.IsNullOrWhiteSpace(input.ProviderRef) || input.ConfirmDuplicate)
            {
                continue;
            }

            var duplicate = await _db.Payments.AsNoTracking().AnyAsync(
                p => p.TenantId == session.TenantId
                    && p.Provider == input.Provider
                    && p.ProviderRef == input.ProviderRef
                    && (cashSessionId.HasValue
                        ? p.CashSessionId == cashSessionId.Value
                        : p.StoreId == session.StoreId && p.BusinessDay == session.BusinessDay),
                cancellationToken);

            if (duplicate)
            {
                return Result<RegisterPaymentsResponse>.Failure(
                    "Já existe um pagamento registrado com esta referência — confirme se não é duplicidade.",
                    ApiErrorCodes.PaymentDuplicateReference);
            }
        }

        var tenantConfig = await _db.TenantConfigs.AsNoTracking().SingleOrDefaultAsync(c => c.TenantId == session.TenantId, cancellationToken);
        var occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;
        var paymentResponses = new List<RegisteredPaymentResponse>(parsedPayments.Count);
        var totalChange = 0m;

        foreach (var (input, method) in parsedPayments)
        {
            var feePercent = PaymentProviderFeePolicy.ResolveFeePercent(tenantConfig?.Payments, input.Provider, method.ToString().ToUpperInvariant());
            var feeAmount = PaymentProviderFeePolicy.CalculateFee(input.Amount, feePercent);
            var netAmount = input.Amount - feeAmount;
            var changeAmount = ChangeEligibleMethods.Contains(input.Method.Trim().ToUpperInvariant()) && input.ReceivedAmount is { } received
                ? received - input.Amount
                : 0m;
            totalChange += changeAmount;

            var payment = Payment.Create(
                session.TenantId,
                session.StoreId,
                session.BusinessDay,
                method,
                amount: input.Amount,
                netAmount: netAmount,
                feeAmount: feeAmount,
                changeAmount: changeAmount,
                installments: input.Installments,
                sessionId: session.Id,
                cashSessionId: cashSessionId,
                provider: input.Provider,
                providerRef: input.ProviderRef,
                cardBrand: input.Brand,
                createdBy: _tenantContext.UserId);

            payment.MarkPaid(occurredAt);
            _db.Payments.Add(payment);

            _db.DomainEvents.Add(DomainEvent.Create(
                session.TenantId,
                type: "payment.registered",
                aggregateType: "payment",
                aggregateId: payment.Id,
                payload: JsonSerializer.Serialize(new
                {
                    sessionId = session.Id,
                    method = input.Method,
                    amount = input.Amount,
                    netAmount,
                    feeAmount,
                    provider = input.Provider,
                    providerRef = input.ProviderRef,
                }),
                origin: _eventOrigin.Origin,
                occurredAt: occurredAt,
                storeId: session.StoreId,
                actorId: _tenantContext.UserId));

            paymentResponses.Add(new RegisteredPaymentResponse(
                payment.Id, input.Method, input.Amount, netAmount, feeAmount, changeAmount,
                input.Provider, input.ProviderRef, payment.ReconciliationStatus.ToString().ToUpperInvariant()));
        }

        session.MarkAsPaid(bill.Subtotal, bill.Discount, bill.ServiceFee, bill.Total);

        _db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "table.session.closed",
            aggregateType: "table_session",
            aggregateId: session.Id,
            payload: JsonSerializer.Serialize(new
            {
                total = bill.Total,
                serviceFee = bill.ServiceFee,
                durationSeconds = (int)(occurredAt - session.OpenedAt).TotalSeconds,
            }),
            origin: _eventOrigin.Origin,
            occurredAt: occurredAt,
            storeId: session.StoreId,
            actorId: _tenantContext.UserId));

        session.Close();
        session.Release();

        var table = await _db.DiningTables.SingleAsync(t => t.Id == session.TableId, cancellationToken);
        table.Release();

        _db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "table.released",
            aggregateType: "dining_table",
            aggregateId: table.Id,
            payload: JsonSerializer.Serialize(new { turnaroundSeconds = (int)(occurredAt - session.OpenedAt).TotalSeconds }),
            origin: _eventOrigin.Origin,
            occurredAt: occurredAt,
            storeId: session.StoreId,
            actorId: _tenantContext.UserId));

        // US-052 §4, cenário "Receita registrada automaticamente" — um lançamento por conta paga
        // (não um por forma de pagamento): o detalhamento por forma já vive em `payment`, o
        // financeiro só precisa saber que a comanda gerou receita.
        var financialEntry = FinancialEntry.Create(
            session.TenantId,
            FinancialEntryType.Revenue,
            bill.Total,
            $"Receita da comanda {session.Id:N}",
            session.BusinessDay,
            storeId: session.StoreId,
            referenceType: "table_session",
            referenceId: session.Id,
            createdBy: _tenantContext.UserId,
            paidAt: occurredAt);
        _db.FinancialEntries.Add(financialEntry);

        _db.AuditLogs.Add(AuditLog.Create(
            session.TenantId,
            action: "PAYMENT_REGISTERED",
            entity: "table_session",
            occurredAt: occurredAt,
            storeId: session.StoreId,
            actorId: _tenantContext.UserId,
            entityId: session.Id,
            after: JsonSerializer.Serialize(new { total = bill.Total, payments = paymentResponses.Count })));

        var response = new RegisterPaymentsResponse(
            new PaymentSessionStatusResponse(session.Status.ToString().ToUpperInvariant()),
            paymentResponses,
            totalChange,
            new ReceiptReferenceResponse($"/v1/sessions/{session.Id}/receipt"));

        return Result<RegisterPaymentsResponse>.Success(response);
    }

    private static bool TryParseMethod(string? method, out PaymentMethod parsed)
    {
        switch (method?.Trim().ToUpperInvariant())
        {
            case "CASH": parsed = PaymentMethod.Cash; return true;
            case "CREDIT": parsed = PaymentMethod.Credit; return true;
            case "DEBIT": parsed = PaymentMethod.Debit; return true;
            case "PIX": parsed = PaymentMethod.Pix; return true;
            case "ONLINE": parsed = PaymentMethod.Online; return true;
            case "VOUCHER": parsed = PaymentMethod.Voucher; return true;
            case "OTHER": parsed = PaymentMethod.Other; return true;
            default: parsed = default; return false;
        }
    }
}
