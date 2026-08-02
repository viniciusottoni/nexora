using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Exercises.Commands.ApproveExercise;
using Awaken.Contracts.Exercises;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Exercises.Commands.RejectExercise;

/// <summary>US-149 (R3.3) — RN-005: qualquer exercício (pending_review ou já approved) pode ser
/// reprovado por um curador, sempre com motivo obrigatório e trilha de auditoria.</summary>
public class RejectExerciseCommandHandler(
    IExerciseCatalogRepository catalogRepository,
    IExerciseRawImportRepository rawImportRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<RejectExerciseCommandHandler> logger) : IRequestHandler<RejectExerciseCommand, RejectExerciseResponse>
{
    public async Task<RejectExerciseResponse> Handle(RejectExerciseCommand request, CancellationToken cancellationToken)
    {
        var catalog = await catalogRepository.GetByIdAsync(request.ExerciseCatalogId, cancellationToken)
            ?? throw new NotFoundException(nameof(ExerciseCatalog), request.ExerciseCatalogId);

        var utcNow = dateTimeService.UtcNow;
        var reviewedBy = ApproveExerciseCommandHandler.ResolveReviewedBy(currentUserService);

        catalog.Reject(request.Reason, reviewedBy, utcNow);
        catalogRepository.Update(catalog);

        if (catalog.RawImportId is { } rawImportId)
        {
            var rawImport = await rawImportRepository.GetByIdAsync(rawImportId, cancellationToken);
            if (rawImport is not null)
            {
                rawImport.MarkRejected(request.Reason, utcNow);
                rawImportRepository.Update(rawImport);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // R4 (analytics): reprovacao de curador - motivo nao entra no log (evita texto livre nos logs).
        logger.LogInformation(
            "exercise_rejected exerciseCatalogId={ExerciseCatalogId} reviewedBy={ReviewedBy}",
            catalog.Id, reviewedBy);

        return new RejectExerciseResponse(catalog.Id, catalog.SanitizationStatus, catalog.RejectionReason!);
    }
}
