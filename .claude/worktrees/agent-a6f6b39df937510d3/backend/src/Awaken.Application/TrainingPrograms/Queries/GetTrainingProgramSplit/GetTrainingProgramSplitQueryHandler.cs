using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.TrainingPrograms;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.TrainingPrograms.Queries.GetTrainingProgramSplit;

public class GetTrainingProgramSplitQueryHandler(ITrainingProgramSplitRepository trainingProgramSplitRepository)
    : IRequestHandler<GetTrainingProgramSplitQuery, TrainingProgramSplitResponse>
{
    public async Task<TrainingProgramSplitResponse> Handle(
        GetTrainingProgramSplitQuery request, CancellationToken cancellationToken)
    {
        var split = await trainingProgramSplitRepository.GetByProgramKeyAsync(request.ProgramKey, cancellationToken);

        if (split is null)
            throw new NotFoundException("TrainingProgramSplit", request.ProgramKey);

        return new TrainingProgramSplitResponse(
            split.ProgramKey,
            split.SplitMapVersion,
            split.Days
                .OrderBy(d => d.DayIndex)
                .Select(d => new TrainingSplitDayResponse(
                    d.DayKey,
                    d.Role,
                    d.LabelI18nKey,
                    d.TargetMuscleGroups,
                    d.TargetMovementPatterns,
                    d.AllowsCoreFinisher))
                .ToList());
    }
}
