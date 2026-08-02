using Awaken.Contracts.Exercises;
using MediatR;

namespace Awaken.Application.Exercises.Queries.GetExerciseRelationships;

/// <summary>
/// US-236 — consulta de candidatos de relação (similares/substitutos/progressões/regressões) de um
/// exercício, ordenados por score, opcionalmente filtrados por categoria.
/// </summary>
public record GetExerciseRelationshipsQuery(
    string Provider,
    string ProviderExerciseId,
    string? Category) : IRequest<IReadOnlyList<ExerciseRelationshipResponse>>;
