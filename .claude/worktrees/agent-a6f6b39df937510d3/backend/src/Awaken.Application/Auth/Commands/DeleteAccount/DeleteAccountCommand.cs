using MediatR;

namespace Awaken.Application.Auth.Commands.DeleteAccount;

public record DeleteAccountCommand : IRequest<Unit>;
