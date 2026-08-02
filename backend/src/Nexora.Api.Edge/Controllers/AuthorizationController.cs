using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Auth.Commands.AuthorizeSensitiveAction;
using Nexora.Contracts.Auth;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Elevação pontual (ADR-023) — porta de AuthorizationController
/// (apps/api-edge/src/modules/auth/authorization.controller.ts). Exige sessão operacional
/// autenticada no terminal (o operador que está pedindo a autorização de um gerente); tenant,
/// loja, ator e dispositivo vêm de <c>ICurrentTenantContext</c>, nunca do corpo.
/// </summary>
[ApiController]
[Route("v1/auth")]
[Authorize]
public sealed class AuthorizationController : ControllerBase
{
    private readonly ISender _sender;

    public AuthorizationController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("authorize")]
    [ProducesResponseType(typeof(AuthorizeSensitiveActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Authorize(
        [FromBody] AuthorizeSensitiveActionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AuthorizeSensitiveActionCommand(request.Action, request.Pin, request.Context);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
