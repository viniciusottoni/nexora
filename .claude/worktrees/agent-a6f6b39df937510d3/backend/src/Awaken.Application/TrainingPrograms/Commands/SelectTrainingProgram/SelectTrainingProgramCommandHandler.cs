using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Progression;
using MediatR;

namespace Awaken.Application.TrainingPrograms.Commands.SelectTrainingProgram;

public class SelectTrainingProgramCommandHandler(
    ITrainingProgramRepository trainingProgramRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IUserWorkoutPreferenceRepository userWorkoutPreferenceRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<SelectTrainingProgramCommand, Unit>
{
    public async Task<Unit> Handle(SelectTrainingProgramCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        var program = await trainingProgramRepository.GetByKeyAsync(request.ProgramKey, cancellationToken)
            ?? throw new NotFoundException("TrainingProgram", request.ProgramKey);

        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? HunterProgression.Create(userId);

        // RN-004/CA-003: validacao server-side obrigatoria mesmo que a UI ja bloqueie.
        if (!RankCalculator.MeetsMinimumRank(progression.Rank, program.MinimumRank))
        {
            throw new ConflictException(
                "RANK_REQUIREMENT_NOT_MET",
                $"Este programa requer rank {RankCalculator.FormatMinimumRank(program.MinimumRank)} ou superior.");
        }

        var existing = await userWorkoutPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        if (existing is null)
        {
            var preference = UserWorkoutPreference.Create(userId, "program", program.Key, utcNow);
            await userWorkoutPreferenceRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            existing.UpdatePreference("program", program.Key, utcNow);
            userWorkoutPreferenceRepository.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
