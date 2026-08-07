using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Platform.Commands.AcknowledgeAttentionItem;
using Nexora.Application.Platform.Queries.ExportAdministrativeAttention;
using Nexora.Application.Platform.Queries.GetAttentionQueue;
using Nexora.Application.Platform.Support;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — fila priorizada de atenção
/// (cross-tenant, por isso NÃO é <c>TenantsController</c>: não é uma rota escopada a um único
/// estabelecimento por id, mesmo raciocínio de <c>PlatformInstallationsController</c>). Todas as
/// rotas exigem <c>PlatformAdmin</c>, mesma policy do restante do módulo de plataforma — RN-015: só
/// metadado técnico/administrativo, nenhum dado de negócio do tenant.
/// </summary>
[ApiController]
[Authorize(Policy = "PlatformAdmin")]
[Route("v1/platform/attention")]
public sealed class PlatformAttentionController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentTenantContext _tenantContext;

    public PlatformAttentionController(ISender sender, ICurrentTenantContext tenantContext)
    {
        _sender = sender;
        _tenantContext = tenantContext;
    }

    private const int DefaultQueueLimit = 25;

    /// <summary>Gherkin "Priorização explicável" — fila ordenada por criticidade, cada item com severidade/motivo/tempo na condição.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AttentionQueueListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery(Name = "severity")] AttentionSeverity[]? severity,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = new GetAttentionQueueQuery(
            severity ?? Array.Empty<AttentionSeverity>(),
            limit ?? DefaultQueueLimit,
            cursor);

        var result = await _sender.Send(query, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Gherkin "Reconhecimento/resolução de pendência administrativa SEM apagar o fato original" (RN-004).</summary>
    [HttpPost("{itemId}/acknowledgements")]
    [ProducesResponseType(typeof(AttentionAcknowledgementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Acknowledge(
        [FromRoute] string itemId,
        [FromBody] AcknowledgeAttentionItemRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new AcknowledgeAttentionItemCommand(itemId, request.Reason, _tenantContext.UserId);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Gherkin implícito "Exportação auditável de metadados administrativos" — CSV para download, mesmo filtro de severidade da fila.</summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery(Name = "severity")] AttentionSeverity[]? severity,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ExportAdministrativeAttentionQuery(
                severity ?? Array.Empty<AttentionSeverity>(),
                _tenantContext.UserId),
            cancellationToken);

        if (result.IsFailure)
            return result.ToActionResult(HttpContext);

        return File(result.Value!.Content, "text/csv", result.Value.FileName);
    }
}
