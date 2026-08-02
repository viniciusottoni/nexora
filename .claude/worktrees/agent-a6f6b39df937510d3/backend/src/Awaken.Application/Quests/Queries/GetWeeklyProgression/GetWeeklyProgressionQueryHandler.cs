using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetWeeklyProgression;

/// US-241: expõe o plano de progressão semanal vigente (mesma reavaliação
/// usada na geração da quest, idempotente dentro da mesma semana/perfil).
public class GetWeeklyProgressionQueryHandler(
    IUserProfileRepository userProfileRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    WeeklyProgressionReviewer reviewer,
    ICurrentUserService currentUserService) : IRequestHandler<GetWeeklyProgressionQuery, WeeklyProgressionResponse>
{
    public async Task<WeeklyProgressionResponse> Handle(
        GetWeeklyProgressionQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("UserProfile", userId);
        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken);

        var plan = await reviewer.ReviewAsync(userId, profile, progression, cancellationToken);

        return new WeeklyProgressionResponse(
            plan.WeekAnchorDate.ToString("yyyy-MM-dd"),
            plan.MesocycleWeekIndex,
            plan.DeloadWeek,
            plan.RecalibratedFromProfileChange,
            progression?.Rank ?? "E",
            plan.Decision,
            plan.Axis,
            plan.VolumeSetsDelta,
            plan.RpeDelta,
            plan.RestSecondsDelta);
    }
}
