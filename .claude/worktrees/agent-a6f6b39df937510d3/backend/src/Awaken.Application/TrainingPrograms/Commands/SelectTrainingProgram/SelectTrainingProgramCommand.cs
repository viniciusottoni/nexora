using MediatR;

namespace Awaken.Application.TrainingPrograms.Commands.SelectTrainingProgram;

public record SelectTrainingProgramCommand(string ProgramKey) : IRequest<Unit>;
