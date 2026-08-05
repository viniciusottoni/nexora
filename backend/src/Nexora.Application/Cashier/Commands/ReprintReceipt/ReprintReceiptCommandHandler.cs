using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Commands.ReprintReceipt;

/// <summary>US-057 §4 — ver docstring de <see cref="ReprintReceiptCommand"/>.</summary>
internal sealed class ReprintReceiptCommandHandler : IRequestHandler<ReprintReceiptCommand, Result<PrintReceiptResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ReprintReceiptCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PrintReceiptResponse>> Handle(ReprintReceiptCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result<PrintReceiptResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        _db.AuditLogs.Add(AuditLog.Create(
            session.TenantId,
            action: "RECEIPT_REPRINTED",
            entity: "table_session",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: session.StoreId,
            actorId: _tenantContext.UserId,
            entityId: session.Id,
            after: JsonSerializer.Serialize(new { printerId = request.PrinterId })));

        return Result<PrintReceiptResponse>.Success(new PrintReceiptResponse(true));
    }
}
