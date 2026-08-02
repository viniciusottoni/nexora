using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Auth.Commands.LoginWithPassword;
using Nexora.Application.Auth.Commands.RefreshToken;
using Nexora.Contracts.Auth;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Http;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Login por senha (+ MFA opcional) e refresh de sessão no cloud (gestor/administrativo) — porta
/// de AuthController (apps/api-cloud/src/modules/auth/auth.controller.ts). Controller fino: só
/// monta o comando e traduz o <c>Result</c> — toda a regra de negócio vive nos handlers
/// <c>LoginWithPasswordCommandHandler</c>/<c>RefreshTokenCommandHandler</c>.
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
    // plumbing): emitir dois pares de token válidos em paralelo não duplica nem corrompe nenhum
    // recurso de negócio — diferente de pedido/pagamento/pareamento de dispositivo. Também roda
    // antes de qualquer tenant resolvido, então exigir a chave aqui só adicionaria fricção sem
    // proteger efeito colateral real.
    [IdempotencyExempt]
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] PasswordLoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginWithPasswordCommand(request.Email, request.Password, request.Otp);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    // Isento de Idempotency-Key — mesma justificativa de Login acima (refresh emitir dois pares
    // de token válidos em paralelo é inofensivo).
    [IdempotencyExempt]
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
