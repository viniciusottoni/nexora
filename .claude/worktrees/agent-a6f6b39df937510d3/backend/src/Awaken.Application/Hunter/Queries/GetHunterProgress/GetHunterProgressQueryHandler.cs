using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Progression;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Hunter.Queries.GetHunterProgress;

public class GetHunterProgressQueryHandler(
    IUserRepository userRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetHunterProgressQuery, ProgressionResponse>
{
    public async Task<ProgressionResponse> Handle(
        GetHunterProgressQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? HunterProgression.Create(user.Id);

        return new ProgressionResponse(
            user.Id,
            progression.Rank,
            progression.RankScore,
            progression.Level,
            progression.TotalXp,
            progression.XpToNextLevel,
            progression.CurrentStreakDays,
            progression.LongestStreakDays,
            new AttributesDto(
                progression.Strength,
                progression.Endurance,
                progression.Agility,
                progression.Vitality,
                progression.Focus,
                progression.Wisdom),
            new AttributeXpDto(
                progression.StrengthXp,
                progression.AgilityXp,
                progression.EnduranceXp,
                progression.VitalityXp,
                progression.FocusXp,
                progression.WisdomXp));
    }
}
