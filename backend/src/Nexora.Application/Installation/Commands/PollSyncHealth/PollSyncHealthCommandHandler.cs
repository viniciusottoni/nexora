using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installation.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Installation.Commands.PollSyncHealth;

internal sealed class PollSyncHealthCommandHandler : IRequestHandler<PollSyncHealthCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ISyncHealthPoller _poller;
    private readonly ILogger<PollSyncHealthCommandHandler> _logger;

    public PollSyncHealthCommandHandler(IApplicationDbContext db, ISyncHealthPoller poller, ILogger<PollSyncHealthCommandHandler> logger)
    {
        _db = db;
        _poller = poller;
        _logger = logger;
    }

    public async Task<Result> Handle(PollSyncHealthCommand request, CancellationToken cancellationToken)
    {
        var installation = await _db.EdgeInstallations.FirstOrDefaultAsync(cancellationToken);
        if (installation is null)
        {
            // Edge ainda não passou pelo bootstrap (ImportBootstrapCommand) — nada a fazer.
            _logger.LogWarning("sync.health.skip: instalação edge ainda não importada.");
            return Result.Success();
        }

        var poll = await _poller.PollAsync(cancellationToken);

        var healthJson = $$"""{"sync":"{{poll.Status}}","httpStatus":{{(poll.HttpStatusCode is { } code ? code : "null")}},"checkedAt":"{{DateTimeOffset.UtcNow:O}}"}""";

        installation.RecordHeartbeat(installation.LastSyncedSeq, clockOffsetMs: null, healthJson);

        _logger.LogInformation("sync.health: {Status}", poll.Status);

        // SaveChangesAsync é feito pelo TransactionBehavior (commands).
        return Result.Success();
    }
}
