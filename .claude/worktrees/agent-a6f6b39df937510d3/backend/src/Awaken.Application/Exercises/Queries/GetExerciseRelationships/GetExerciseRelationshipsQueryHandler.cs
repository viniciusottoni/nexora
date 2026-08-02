using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.Exercises;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Exercises.Queries.GetExerciseRelationships;

public class GetExerciseRelationshipsQueryHandler(IExerciseCatalogRepository repository)
    : IRequestHandler<GetExerciseRelationshipsQuery, IReadOnlyList<ExerciseRelationshipResponse>>
{
    public async Task<IReadOnlyList<ExerciseRelationshipResponse>> Handle(
        GetExerciseRelationshipsQuery request,
        CancellationToken cancellationToken)
    {
        var exercise = await repository.GetByProviderExerciseIdAsync(
            request.Provider,
            request.ProviderExerciseId,
            cancellationToken);

        if (exercise is null)
            throw new NotFoundException("Exercise", request.ProviderExerciseId);

        var relations = exercise.Relations.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Category))
            relations = relations.Where(relation =>
                string.Equals(relation.RelationKind, request.Category, StringComparison.OrdinalIgnoreCase));

        return relations
            .OrderByDescending(relation => relation.Score)
            .Select(relation => new ExerciseRelationshipResponse(
                relation.RelatedProviderExerciseId,
                relation.RelatedName,
                relation.RelationKind,
                relation.Types,
                relation.Score,
                relation.Confidence))
            .ToList();
    }
}
