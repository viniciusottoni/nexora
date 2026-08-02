using Awaken.Contracts.Exercises;
using MediatR;

namespace Awaken.Application.Exercises.Commands.ApproveExercise;

public record ApproveExerciseCommand(Guid ExerciseCatalogId) : IRequest<ApproveExerciseResponse>;
