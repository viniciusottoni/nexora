using Awaken.Contracts.Exercises;
using MediatR;

namespace Awaken.Application.Exercises.Commands.ImportExercises;

public record ImportExercisesCommand(
    string BatchKey,
    string Provider,
    int? MaxFiles,
    bool ApproveOnImport,
    string? DatasetVersion = null) : IRequest<ImportExercisesResponse>;
