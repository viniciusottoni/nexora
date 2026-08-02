using Awaken.Application.Common.Exceptions;
using Awaken.Application.Exercises;
using Awaken.Contracts.Exercises;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Exercises.Queries.GetExerciseCatalog;

public class GetExerciseCatalogQueryHandler(IExerciseCatalogRepository repository)
    : IRequestHandler<GetExerciseCatalogQuery, ExerciseCatalogResponse>
{
    public async Task<ExerciseCatalogResponse> Handle(
        GetExerciseCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var exercise = await repository.GetByProviderExerciseIdAsync(
            request.Provider,
            request.ProviderExerciseId,
            cancellationToken);

        if (exercise is null)
            throw new NotFoundException("Exercise", request.ProviderExerciseId);

        return exercise.ToResponse();
    }
}
