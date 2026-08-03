using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Audit.Commands.RecordAuditLogAccess;
using Nexora.Application.Audit.Queries.GetAuditLog;
using Nexora.Contracts.Audit;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// E-09/US-091 — consulta e filtro da trilha de auditoria pelo gestor. Autoridade do dado é a
/// nuvem (US-091, "Autoridade do dado: Nuvem"); a trilha local do edge existe e é consultável pelo
/// painel local em caso de necessidade operacional, mas a investigação gerencial acontece aqui.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/audit")]
public sealed class AuditController : ControllerBase
{
    private const int DefaultLimit = 50;

    private readonly ISender _sender;
    private readonly ILogger<AuditController> _logger;

    public AuditController(ISender sender, ILogger<AuditController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    /// Lista a trilha filtrada e paginada por cursor. O próprio acesso é registrado
    /// (cenário Gherkin "Acesso restrito e registrado") — por isso esta ação dispara dois comandos
    /// MediatR: a consulta em si e <see cref="RecordAuditLogAccessCommand"/>, nesta ordem. Uma
    /// falha ao registrar o acesso é logada mas NUNCA vira erro para o chamador — a policy já
    /// autorizou a consulta, e um defeito no registro de "quem consultou a trilha" não deveria
    /// impedir a própria consulta de funcionar.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AuditRead")]
    [ProducesResponseType(typeof(AuditLogListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? actorId,
        [FromQuery] Guid? authorizedBy,
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] Guid? entityId,
        [FromQuery] decimal? minAmount,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = new GetAuditLogQuery(
            from, to, actorId, authorizedBy, action, entity, entityId, minAmount, limit ?? DefaultLimit, cursor);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            var filters = new Dictionary<string, string?>
            {
                ["from"] = from?.ToString("O"),
                ["to"] = to?.ToString("O"),
                ["actorId"] = actorId?.ToString(),
                ["authorizedBy"] = authorizedBy?.ToString(),
                ["action"] = action,
                ["entity"] = entity,
                ["entityId"] = entityId?.ToString(),
            };

            try
            {
                await _sender.Send(new RecordAuditLogAccessCommand(filters), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha ao registrar o acesso à trilha de auditoria (a consulta em si foi bem-sucedida).");
            }
        }

        return result.ToActionResult(HttpContext);
    }
}
