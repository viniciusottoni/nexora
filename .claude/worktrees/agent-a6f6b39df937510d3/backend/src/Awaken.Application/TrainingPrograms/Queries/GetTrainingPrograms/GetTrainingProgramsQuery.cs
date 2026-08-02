using Awaken.Contracts.TrainingPrograms;
using MediatR;

namespace Awaken.Application.TrainingPrograms.Queries.GetTrainingPrograms;

public record GetTrainingProgramsQuery : IRequest<IReadOnlyList<TrainingProgramResponse>>;
