using Awaken.Contracts.Admin.Auth;
using MediatR;

namespace Awaken.Application.Admin.Auth.Commands.AdminMfaSetup;

public record AdminMfaSetupCommand(Guid AdminUserId) : IRequest<AdminMfaSetupResponse>;
