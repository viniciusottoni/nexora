using Awaken.Contracts.Exercises;
using MediatR;

namespace Awaken.Application.Exercises.Queries.GetExerciseCatalog;

public record GetExerciseCatalogQuery(string Provider, string ProviderExerciseId) : IRequest<ExerciseCatalogResponse>;
