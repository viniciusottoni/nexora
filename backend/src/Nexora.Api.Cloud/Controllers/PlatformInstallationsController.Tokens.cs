using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Installations.Commands.ReissueInstallationToken;
using Nexora.Application.Installations.Commands.RevokeInstallationCredential;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Http;
using Nexora.Contracts.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — reemissão/revogação de
/// credencial de instalação. Partial file NOVO (ver docstring de <see cref="PlatformInstallationsController"/>)
/// para não colidir com outras histórias da E-15 editando o arquivo principal em paralelo. Mesma
/// policy <c>PlatformAdmin</c> do restante do controller — não há equivalente self-service (só a
/// plataforma reemite/revoga credencial de instalação).
/// </summary>
public partial class PlatformInstallationsController
{
    /// <summary>
    /// US-156 §Gherkin "Resposta de criação foi perdida"/"Reemissão segura" — revoga qualquer
    /// credencial pendente anterior da MESMA instalação e emite uma nova, exibida uma ÚNICA vez
    /// nesta resposta. <c>Idempotency-Key</c> obrigatório (ADR-020, middleware global); a resposta
    /// ARMAZENADA para um eventual reenvio da mesma chave tem <c>installToken</c>/
    /// <c>installCommand</c> trocados por <c>null</c> (ver <see cref="IdempotencyRedactFieldsAttribute"/>
    /// — decisão registrada no relatório desta tarefa para a pendência do doc. de spec §15).
    /// </summary>
    [HttpPost("{installationId:guid}/tokens")]
    [IdempotencyRedactFields("installToken", "installCommand")]
    [ProducesResponseType(typeof(ReissueInstallationTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReissueToken(
        [FromRoute] Guid installationId,
        [FromBody] ReissueInstallationTokenRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] ICurrentTenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var command = new ReissueInstallationTokenCommand(
            installationId, request.Reason, request.ExpiresInHours, tenantContext.UserId);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-156 "revogação manual de token comprometido" — idempotente (revogar de novo não é erro).</summary>
    [HttpDelete("{installationId:guid}/tokens/{credentialId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RevokeToken(
        [FromRoute] Guid installationId,
        [FromRoute] Guid credentialId,
        [FromBody] RevokeInstallationCredentialRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] ICurrentTenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var command = new RevokeInstallationCredentialCommand(
            installationId, credentialId, request.Reason, tenantContext.UserId);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
