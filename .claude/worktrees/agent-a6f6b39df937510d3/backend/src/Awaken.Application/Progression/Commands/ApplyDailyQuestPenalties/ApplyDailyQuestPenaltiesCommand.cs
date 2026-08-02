using MediatR;
using Awaken.Application.Progression.Services;

namespace Awaken.Application.Progression.Commands.ApplyDailyQuestPenalties;

/// US-129: aciona, na virada de dia, a penalidade de XP (EPIC-009/US-132) para dailies nao completadas.
public record ApplyDailyQuestPenaltiesCommand : IRequest<DailyPenaltyRolloverSummary>;
