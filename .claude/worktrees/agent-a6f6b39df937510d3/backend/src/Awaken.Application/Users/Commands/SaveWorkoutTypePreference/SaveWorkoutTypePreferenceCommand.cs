using MediatR;

namespace Awaken.Application.Users.Commands.SaveWorkoutTypePreference;

public record SaveWorkoutTypePreferenceCommand(string PreferredTrainingType, string? PreferredProgramId)
    : IRequest<Unit>;
