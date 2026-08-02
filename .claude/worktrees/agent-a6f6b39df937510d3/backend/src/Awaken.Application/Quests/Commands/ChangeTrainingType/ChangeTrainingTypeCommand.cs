using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Commands.ChangeTrainingType;

public record ChangeTrainingTypeCommand(Guid QuestId, string TrainingType, string? ProgramId)
    : IRequest<QuestPreviewResponse>;
