using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Nutrition;
using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Nutrition.Commands.UpdateCupVolume;

public class UpdateCupVolumeCommandHandler(
    ICurrentUserService currentUserService,
    IUserNutritionPreferenceRepository preferenceRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateCupVolumeCommand, UpdateCupVolumeResponse>
{
    public async Task<UpdateCupVolumeResponse> Handle(
        UpdateCupVolumeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var preference = await preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        // US-090 RN-004: cria preferência na primeira vez.
        if (preference is null)
        {
            preference = UserNutritionPreference.Create(userId, request.CupVolumeMl);
            await preferenceRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            preference.UpdateCupVolume(request.CupVolumeMl);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateCupVolumeResponse(CupVolumeMl: preference.CupVolumeMl);
    }
}
