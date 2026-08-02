using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Exercises;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Exercises.Commands.ApproveExercise;

/// <summary>
/// US-149 (R3.3) — portão final de curadoria manual. Reaproveita
/// <see cref="ExerciseCatalog.ApproveForWorkoutGeneration"/>, que já lança
/// <see cref="InvalidOperationException"/> quando <see cref="ExerciseCatalog.CanBeApproved"/> é falso
/// (RN-001: sem nome PT-BR/músculo/equipamento/mídia/instrução válidos, ou com pendência de sanitização).
/// </summary>
public class ApproveExerciseCommandHandler(
    IExerciseCatalogRepository catalogRepository,
    IExerciseRawImportRepository rawImportRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<ApproveExerciseCommandHandler> logger) : IRequestHandler<ApproveExerciseCommand, ApproveExerciseResponse>
{
    public async Task<ApproveExerciseResponse> Handle(ApproveExerciseCommand request, CancellationToken cancellationToken)
    {
        var catalog = await catalogRepository.GetByIdAsync(request.ExerciseCatalogId, cancellationToken)
            ?? throw new NotFoundException(nameof(ExerciseCatalog), request.ExerciseCatalogId);

        var utcNow = dateTimeService.UtcNow;
        var reviewedBy = ResolveReviewedBy(currentUserService);

        catalog.ApproveForWorkoutGeneration(utcNow, reviewedBy);
        catalogRepository.Update(catalog);

        if (catalog.RawImportId is { } rawImportId)
        {
            var rawImport = await rawImportRepository.GetByIdAsync(rawImportId, cancellationToken);
            if (rawImport is not null)
            {
                rawImport.MarkApproved(utcNow);
                rawImportRepository.Update(rawImport);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // R4 (analytics): aprovação manual de curador (distinta da auto-aprovação no import).
        logger.LogInformation(
            "exercise_approved exerciseCatalogId={ExerciseCatalogId} source=curator reviewedBy={ReviewedBy}",
            catalog.Id, reviewedBy);

        return new ApproveExerciseResponse(catalog.Id, catalog.SanitizationStatus, catalog.IsApprovedForWorkoutGeneration);
    }

    internal static string ResolveReviewedBy(ICurrentUserService currentUserService) =>
        currentUserService.Email ?? currentUserService.UserId.ToString();
}
