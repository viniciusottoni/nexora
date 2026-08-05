using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tenants.Commands.RevokeSupportAccess;
using Nexora.Application.Tenants.Queries.GetSupportAccessHistory;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-145 "Acesso de suporte auditado" — lado do CLIENTE: consultar o histórico de acessos de
/// suporte da própria loja (§10, "sem precisar pedir") e revogar um acesso ativo (§4, cenário
/// "Revogação pelo cliente"). <c>[Authorize]</c> genérico (qualquer usuário autenticado do próprio
/// tenant) — diferente de <see cref="SupportAccessController"/>, que exige <c>PlatformAdmin</c>;
/// aqui é o gestor do estabelecimento, resolvido por <see cref="ICurrentTenantContext"/>, nunca
/// por um id vindo da requisição (ADR "tenant nunca vem do cliente").
/// </summary>
[ApiController]
[Authorize]
[Route("v1/tenant")]
public sealed class TenantSupportAccessController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentTenantContext _tenantContext;

    public TenantSupportAccessController(ISender sender, ICurrentTenantContext tenantContext)
    {
        _sender = sender;
        _tenantContext = tenantContext;
    }

    /// <summary>US-145 §10 — histórico completo do próprio tenant, mais recente primeiro.</summary>
    [HttpGet("support-access-history")]
    [ProducesResponseType(typeof(SupportAccessListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSupportAccessHistoryQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-145 §4, cenário "Revogação pelo cliente" — cessa o acesso imediatamente. Id de outra
    /// concessão (de outro tenant, RLS à parte) devolve 404, nunca 403 (ADR-021, "não revelar que
    /// o recurso existe em outro tenant").
    /// </summary>
    [HttpDelete("support-access/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is not { } tenantId)
        {
            return Result.Failure("Não foi possível identificar o estabelecimento vinculado ao seu usuário.", ApiErrorCodes.TenantContextMissing)
                .ToActionResult(HttpContext);
        }

        var command = new RevokeSupportAccessCommand(tenantId, id, _tenantContext.UserId);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
