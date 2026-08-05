using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Platform.Queries.ListSupportAccessReport;
using Nexora.Application.Tenants.Commands.RecordSupportAccess;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-145 "Acesso de suporte auditado" — lado da PLATAFORMA (Replay): solicitar acesso a um tenant
/// e consultar o relatório consolidado. Extraído de <see cref="TenantsController"/> (que tinha uma
/// versão anterior desta ação, E-09/US-090, sem token/ciclo de vida real) para um controller
/// próprio porque esta US introduz um módulo inteiro (concessão real com token, revogação,
/// histórico, relatório) — o lado do CLIENTE (revogar/consultar histórico) fica em
/// <see cref="TenantSupportAccessController"/>, com policy de autorização diferente
/// (<c>PlatformAdmin</c> aqui, tenant autenticado lá).
/// </summary>
[ApiController]
[Authorize(Policy = "PlatformAdmin")]
public sealed class SupportAccessController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentTenantContext _tenantContext;

    public SupportAccessController(ISender sender, ICurrentTenantContext tenantContext)
    {
        _sender = sender;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// US-145 §4, cenário "Acesso concedido com registro" — gera token de escopo especial com
    /// expiração curta, grava a concessão (revogável pelo cliente), registra em
    /// <c>audit_log</c>/EVT-074 (E-09/US-090) e notifica o(s) OWNER(s) do tenant no mesmo instante
    /// (§10, "não depois"). O raw token só existe nesta resposta — nunca persistido.
    /// </summary>
    [HttpPost("v1/platform/tenants/{id:guid}/support-access")]
    [ProducesResponseType(typeof(GrantSupportAccessResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Grant(
        [FromRoute] Guid id,
        [FromBody] GrantSupportAccessRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordSupportAccessCommand(id, _tenantContext.UserId, request.Reason, request.DurationMinutes);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created };
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-145 §11 "Acessos de suporte por tenant e por período" — relatório consolidado (MVP: sem
    /// paginação por cursor, ver docstring de <see cref="ListSupportAccessReportQuery"/>).
    /// </summary>
    [HttpGet("v1/platform/support-access")]
    [ProducesResponseType(typeof(SupportAccessListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Report(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListSupportAccessReportQuery(tenantId, from, to), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
