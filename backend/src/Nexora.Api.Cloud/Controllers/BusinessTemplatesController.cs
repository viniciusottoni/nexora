using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Provisioning.Commands.UpdateBusinessTemplate;
using Nexora.Application.Provisioning.Queries.GetBusinessTemplate;
using Nexora.Application.Provisioning.Queries.ListBusinessTemplates;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Catálogo de modelos de negócio (US-142) — pizzaria, hamburgueria, restaurante, lanchonete,
/// mantidos pela Replay e aplicados na criação de um tenant (<see cref="TenantsController"/>).
/// Módulo de plataforma (a US não lista nenhuma ação do cliente final): as três rotas exigem
/// <c>PlatformAdmin</c>, mesma exceção legítima do ADR-013 usada por <c>TenantDomainsController</c>.
/// </summary>
[ApiController]
[Authorize(Policy = "PlatformAdmin")]
[Route("v1/platform/templates")]
public sealed class BusinessTemplatesController : ControllerBase
{
    private readonly ISender _sender;

    public BusinessTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista os modelos ativos — usado pelo seletor da tela de provisionamento (US-142 §7).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(BusinessTemplateListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListBusinessTemplatesQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Detalhe completo (config+seeds) de um modelo — tela de manutenção da Replay (US-142 §10).</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(BusinessTemplateDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string code, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBusinessTemplateQuery(code), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Edita o modelo (incrementa a versão) — tenants já provisionados não são afetados (US-142 §4, cenário "Atualização de modelo").</summary>
    [HttpPut("{code}")]
    [ProducesResponseType(typeof(BusinessTemplateDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] string code,
        [FromBody] UpdateBusinessTemplateRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateBusinessTemplateCommand(code, request.Name, request.ConfigJson, request.SeedsJson),
            cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
