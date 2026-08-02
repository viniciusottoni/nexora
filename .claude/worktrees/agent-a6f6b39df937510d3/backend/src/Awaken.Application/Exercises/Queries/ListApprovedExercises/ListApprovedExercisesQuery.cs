using Awaken.Contracts.Exercises;
using MediatR;

namespace Awaken.Application.Exercises.Queries.ListApprovedExercises;

public record ListApprovedExercisesQuery : IRequest<IReadOnlyList<ExerciseCatalogResponse>>;
