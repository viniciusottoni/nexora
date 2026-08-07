using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Tenants.Commands.ReissueOwnerInvite;
using Nexora.Application.Tenants.Commands.RevokeOwnerInvite;
using Nexora.Application.Tenants.Commands.TransferTenantOwnership;
using Nexora.Application.Tenants.Commands.UnlockOwnerAccess;
using Nexora.Application.Tenants.Queries.GetTenantOwnership;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — camada ADMINISTRATIVA sobre o convite de
/// dono já existente (criado no provisionamento, US-002, e aceito por
/// <c>OwnerInvitationsController</c>): consultar, reenviar/corrigir, revogar, transferir titularidade
/// e desbloquear o acesso — nunca ver/definir senha (fora de escopo, ver docstring de
/// <see cref="UnlockOwnership"/>). Todos os endpoints são exclusivos de <c>PlatformAdmin</c> — não há
/// self-service equivalente (mesma justificativa de <see cref="List"/>/<see cref="Overview"/>).
/// </summary>
public partial class TenantsController
{
    /// <summary>Estado do acesso inicial do proprietário: quem é, histórico de convites e de transferências (US-155).</summary>
    [HttpGet("{id:guid}/ownership")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TenantOwnershipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ownership([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantOwnershipQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Reenvia (mesmo e-mail) ou corrige (e-mail/nome diferentes) o convite pendente do proprietário —
    /// ver docstring de <see cref="ReissueOwnerInviteCommand"/> sobre por que os dois cenários Gherkin
    /// ("Convite expirado" e "E-mail corrigido") compartilham este único endpoint/comando.
    /// </summary>
    [HttpPost("{id:guid}/owner-invites")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(CreateOwnerInviteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateOwnerInvite(
        [FromRoute] Guid id,
        [FromBody] CreateOwnerInviteRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new ReissueOwnerInviteCommand(id, request.Name, request.Email, request.Reason, _tenantContext.UserId);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(Ownership), new { id }, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Revoga um convite de dono ainda pendente (não aceito) — endpoint adicional exigido pelo escopo da US, não listado no contrato abreviado.</summary>
    [HttpDelete("{id:guid}/owner-invites/{inviteId:guid}")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RevokeOwnerInvite(
        [FromRoute] Guid id,
        [FromRoute] Guid inviteId,
        [FromBody] RevokeOwnerInviteRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new RevokeOwnerInviteCommand(id, inviteId, request.Reason, _tenantContext.UserId);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Transfere o papel OWNER para outro usuário do mesmo estabelecimento (Gherkin "Transferência de titularidade").</summary>
    [HttpPost("{id:guid}/ownership-transfers")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(TransferTenantOwnershipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> TransferOwnership(
        [FromRoute] Guid id,
        [FromBody] TransferTenantOwnershipRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new TransferTenantOwnershipCommand(
            id, request.NewOwnerUserId, request.Reason, request.KeepPreviousAsAdmin, _tenantContext.UserId);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Desbloqueio administrativo do proprietário — endpoint adicional exigido pelo escopo da US.
    /// NUNCA define ou visualiza senha (fora de escopo explícito da US): só reverte o bloqueio de
    /// conta (<c>AppUser.Unblock</c>); a senha existente do proprietário continua a mesma.
    /// </summary>
    [HttpPost("{id:guid}/ownership/unlock")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(UnlockOwnerAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UnlockOwnership(
        [FromRoute] Guid id,
        [FromBody] UnlockOwnerAccessRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new UnlockOwnerAccessCommand(id, request.Reason, _tenantContext.UserId);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
