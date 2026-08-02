using Awaken.Contracts.Exercises;
using MediatR;

namespace Awaken.Application.Exercises.Commands.RejectExercise;

public record RejectExerciseCommand(Guid ExerciseCatalogId, string Reason) : IRequest<RejectExerciseResponse>;
