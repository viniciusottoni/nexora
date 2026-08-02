using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Auth.Commands.LoginWithPin;
using Nexora.Contracts.Auth;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Http;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Login por PIN no terminal do edge (ADR-014) — porta de AuthController
/// (apps/api-edge/src/modules/auth/auth.controller.ts). Controller fino: só monta o comando e
/// traduz o <c>Result</c> — toda a regra de negócio vive em
/// <c>LoginWithPinCommandHandler</c>.
/// </summary>
[ApiController]
[Route("v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    // Isento de Idempotency-Key (ADR-020, decisão registrada no relatório da tarefa de
    // plumbing): emitir dois tokens de sessão válidos em paralelo é inofensivo — diferente de
    // pareamento de dispositivo (POST /v1/devices/pair), que continua exigindo a chave por
    // registrar um recurso novo.
    [IdempotencyExempt]
    [HttpPost("pin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PinLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Pin(
        [FromBody] PinLoginRequest request,
        CancellationToken cancellationToken)
    {
        var deviceSecret = Request.Headers["X-Device-Secret"].FirstOrDefault();
        var command = new LoginWithPinCommand(request.DeviceId, request.Pin, deviceSecret);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
