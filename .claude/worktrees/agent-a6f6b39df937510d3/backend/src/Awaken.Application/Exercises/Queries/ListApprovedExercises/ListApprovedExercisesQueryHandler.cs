using Awaken.Application.Exercises;
using Awaken.Contracts.Exercises;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Exercises.Queries.ListApprovedExercises;

public class ListApprovedExercisesQueryHandler(IExerciseCatalogRepository repository)
    : IRequestHandler<ListApprovedExercisesQuery, IReadOnlyList<ExerciseCatalogResponse>>
{
    public async Task<IReadOnlyList<ExerciseCatalogResponse>> Handle(
        ListApprovedExercisesQuery request,
        CancellationToken cancellationToken)
    {
        var exercises = await repository.ListApprovedForWorkoutGenerationAsync(cancellationToken);
        return exercises.Select(exercise => exercise.ToResponse()).ToList();
    }
}
