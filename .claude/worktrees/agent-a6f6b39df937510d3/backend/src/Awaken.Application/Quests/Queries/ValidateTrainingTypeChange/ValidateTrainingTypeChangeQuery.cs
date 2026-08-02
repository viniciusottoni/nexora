using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;

public record ValidateTrainingTypeChangeQuery(Guid QuestId, string TrainingType, string? ProgramId)
    : IRequest<ValidateTrainingTypeChangeResponse>;
