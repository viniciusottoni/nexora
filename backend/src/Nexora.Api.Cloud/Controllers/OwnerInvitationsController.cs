using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Tenants.Commands.AcceptOwnerInvitation;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Aceite de convite de dono enviado no provisionamento — rota pública (o token no corpo É a
/// credencial, não há sessão prévia). Porta de <c>owner-invitations.controller.ts</c>.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("v1/auth/invitations")]
public sealed class OwnerInvitationsController : ControllerBase
{
    private readonly ISender _sender;

    public OwnerInvitationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Define a senha inicial do dono e ativa a conta.</summary>
    [HttpPost("accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Accept(
        [FromBody] AcceptOwnerInviteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AcceptOwnerInvitationCommand(request.Token, request.Password);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
