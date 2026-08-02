using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.TrainingPrograms;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Progression;
using MediatR;

namespace Awaken.Application.TrainingPrograms.Queries.GetTrainingPrograms;

public class GetTrainingProgramsQueryHandler(
    ITrainingProgramRepository trainingProgramRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IUserWorkoutPreferenceRepository userWorkoutPreferenceRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetTrainingProgramsQuery, IReadOnlyList<TrainingProgramResponse>>
{
    public async Task<IReadOnlyList<TrainingProgramResponse>> Handle(
        GetTrainingProgramsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var programs = await trainingProgramRepository.GetActiveProgramsAsync(cancellationToken);

        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? HunterProgression.Create(userId);

        var preference = await userWorkoutPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        return programs
            .Select(program => new TrainingProgramResponse(
                program.Id,
                program.Key,
                program.Name,
                program.Description,
                program.Category,
                RankCalculator.FormatMinimumRank(program.MinimumRank),
                program.SplitDays,
                RankCalculator.MeetsMinimumRank(progression.Rank, program.MinimumRank),
                preference?.PreferredTrainingType == "program" && preference.PreferredProgramId == program.Key))
            .ToList();
    }
}
