using MediatR;

namespace Awaken.Application.Users.Commands.SelectAvatar;

public record SelectAvatarCommand(string AvatarKey) : IRequest<Unit>;
