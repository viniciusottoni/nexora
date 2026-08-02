using Awaken.Contracts.Admin.Auth;
using MediatR;

namespace Awaken.Application.Admin.Auth.Commands.AdminMfaVerify;

public record AdminMfaVerifyCommand(Guid AdminUserId, string Code) : IRequest<AdminMfaVerifyResponse>;
