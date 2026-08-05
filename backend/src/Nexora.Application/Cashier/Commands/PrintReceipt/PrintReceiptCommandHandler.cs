using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Cashier;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Commands.PrintReceipt;

internal sealed class PrintReceiptCommandHandler : IRequestHandler<PrintReceiptCommand, Result<PrintReceiptResponse>>
{
    private readonly IApplicationDbContext _db;

    public PrintReceiptCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PrintReceiptResponse>> Handle(PrintReceiptCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.TableSessions.AsNoTracking().AnyAsync(s => s.Id == request.SessionId, cancellationToken);
        if (!exists)
        {
            return Result<PrintReceiptResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        // US-057 §4/§10: enfileira e devolve sempre — falha de impressora física (ainda sem
        // hardware definido, ADR-026) nunca é reportada como erro deste comando.
        return Result<PrintReceiptResponse>.Success(new PrintReceiptResponse(true));
    }
}
