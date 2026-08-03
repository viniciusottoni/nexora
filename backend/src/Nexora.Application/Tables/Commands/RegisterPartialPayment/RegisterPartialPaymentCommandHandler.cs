using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Cashier;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.RegisterPartialPayment;

/// <summary>US-027 §4, cenário "Divisão por valor" — ver docstring de <see cref="RegisterPartialPaymentCommand"/>.</summary>
internal sealed class RegisterPartialPaymentCommandHandler : IRequestHandler<RegisterPartialPaymentCommand, Result<PartialPaymentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;

    public RegisterPartialPaymentCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IEventOriginProvider eventOrigin)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
    }

    public async Task<Result<PartialPaymentResponse>> Handle(RegisterPartialPaymentCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions
            .SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
        {
            return Result<PartialPaymentResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        if (session.Status != TableSessionStatus.BillRequested)
        {
            return Result<PartialPaymentResponse>.Failure(
                "Só é possível registrar um pagamento parcial depois que a conta foi solicitada.", ApiErrorCodes.BillNotRequested);
        }

        if (!TryParseMethod(request.Method, out var method))
        {
            return Result<PartialPaymentResponse>.Failure(
                "Forma de pagamento inválida.", ApiErrorCodes.BillInvalidAmount);
        }

        var items = await BillQueryCoordinator.LoadItemsAsync(_db, session.Id, cancellationToken);
        var feePercent = await BillQueryCoordinator.ResolveFeePercentAsync(_db, session.TenantId, cancellationToken);
        var subtotal = items.Where(i => i.Status != OrderItemStatus.Cancelled).Sum(i => i.TotalPrice);
        var serviceFee = Math.Round(subtotal * feePercent / 100m, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + serviceFee;

        var alreadyPaid = await BillQueryCoordinator.SumPaidAsync(_db, session.Id, cancellationToken);
        var openBalance = total - alreadyPaid;

        if (request.Amount <= 0 || request.Amount > openBalance)
        {
            return Result<PartialPaymentResponse>.Failure(
                "O valor pago precisa ser maior que zero e não pode exceder o saldo em aberto.", ApiErrorCodes.BillInvalidAmount);
        }

        var payment = Payment.Create(
            session.TenantId,
            session.StoreId,
            session.BusinessDay,
            method,
            amount: request.Amount,
            netAmount: request.Amount,
            sessionId: session.Id,
            createdBy: _tenantContext.UserId);

        // Registro manual do caixa (dinheiro/PIX/cartão recebido na hora) — não passa por
        // autorização de gateway nesta história, por isso vai direto para Paid (mesma semântica de
        // "dinheiro em mãos" que qualquer PDV físico usa).
        payment.MarkPaid(DateTimeOffset.UtcNow);
        _db.Payments.Add(payment);

        // ADR-006: toda transição/fato de negócio novo emite seu evento na mesma transação. Nome
        // deliberadamente distinto de um futuro EVT-032 "payment.registered" de US-052 (fechamento
        // completo, fora deste épico) — este é especificamente o pagamento PARCIAL da divisão por
        // valor (US-027 §3.2 exclui "registro do pagamento propriamente dito" do fechamento final,
        // mas o pagamento parcial em si É o objeto desta história, não uma antecipação da US-052).
        _db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "table.session.partial_payment_registered",
            aggregateType: "payment",
            aggregateId: payment.Id,
            payload: JsonSerializer.Serialize(new { sessionId = session.Id, amount = request.Amount, method = request.Method }),
            origin: _eventOrigin.Origin,
            occurredAt: DateTimeOffset.UtcNow,
            storeId: session.StoreId,
            actorId: _tenantContext.UserId));

        _db.AuditLogs.Add(AuditLog.Create(
            session.TenantId,
            action: "TABLE_SESSION_PARTIAL_PAYMENT_REGISTERED",
            entity: "table_session",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: session.StoreId,
            actorId: _tenantContext.UserId,
            entityId: session.Id,
            after: JsonSerializer.Serialize(new { paymentId = payment.Id, amount = request.Amount, method = request.Method })));

        var remaining = openBalance - request.Amount;

        return Result<PartialPaymentResponse>.Success(new PartialPaymentResponse(
            payment.Id, request.Amount, remaining, total, session.Status.ToString().ToUpperInvariant()));
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
