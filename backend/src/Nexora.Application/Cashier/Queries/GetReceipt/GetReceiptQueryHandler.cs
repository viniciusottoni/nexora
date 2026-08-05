using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Queries.GetReceipt;

/// <summary>US-057 §4/§7 — ver docstring de <see cref="GetReceiptQuery"/>.</summary>
internal sealed class GetReceiptQueryHandler : IRequestHandler<GetReceiptQuery, Result<GetReceiptResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetReceiptQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<GetReceiptResponse>> Handle(GetReceiptQuery request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result<GetReceiptResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        if (session.Status is not (TableSessionStatus.Paid or TableSessionStatus.Closed))
        {
            return Result<GetReceiptResponse>.Failure(
                "O comprovante só fica disponível depois que a conta é paga.", ApiErrorCodes.TableSessionNotOpen);
        }

        var items = await BillQueryCoordinator.LoadItemsAsync(_db, session.Id, cancellationToken);
        var itemResponses = items.Select(i => BillQueryCoordinator.BuildItemResponse(i)).ToList();

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.SessionId == session.Id && p.Status == PaymentStatus.Paid)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new ReceiptPaymentResponse(p.Method.ToString().ToUpperInvariant(), p.Amount))
            .ToListAsync(cancellationToken);

        var receipt = new ReceiptResponse(
            Url: $"/v1/sessions/{session.Id}/receipt",
            // Não fiscal (RN-023, pendência crítica) — número de apresentação, não sequência fiscal.
            Number: $"NF-{session.BusinessDay:yyyyMMdd}-{session.Id.ToString("N")[..6].ToUpperInvariant()}",
            IsFiscal: false,
            IssuedAt: session.ClosedAt ?? session.UpdatedAt,
            Items: itemResponses,
            Payments: payments,
            Subtotal: session.Subtotal,
            ServiceFee: session.ServiceFeeAmount,
            Discount: session.DiscountAmount,
            Total: session.TotalAmount);

        return Result<GetReceiptResponse>.Success(new GetReceiptResponse(receipt));
    }
}
