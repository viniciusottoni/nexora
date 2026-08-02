using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Users.Commands.SaveWorkoutTypePreference;

public class SaveWorkoutTypePreferenceCommandHandler(
    IUserWorkoutPreferenceRepository preferenceRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<SaveWorkoutTypePreferenceCommand, Unit>
{
    public async Task<Unit> Handle(SaveWorkoutTypePreferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        // RN-002/RN-003: programId so faz sentido para o tipo 'program'.
        var programId = request.PreferredTrainingType == "program"
            ? request.PreferredProgramId
            : null;

        var existing = await preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        if (existing is null)
        {
            var preference = UserWorkoutPreference.Create(
                userId, request.PreferredTrainingType, programId, utcNow);
            await preferenceRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            existing.UpdatePreference(request.PreferredTrainingType, programId, utcNow);
            preferenceRepository.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
