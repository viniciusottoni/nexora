using System.Diagnostics;
using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.Prices.Commands.BulkAdjustPricesByCategory;
using Nexora.Application.Catalog.Prices.Commands.SetVariantChannelPrice;
using Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Preço por canal de venda (US-014) — tabela de preço por canal de uma variante e reajuste em
/// massa por categoria. Sub-recurso de catálogo, mesma família de permissão de
/// <c>ProductsController</c>/<c>ProductVariantsController</c> (US-010/US-011): reaproveita as
/// policies <c>ProductRead</c>/<c>ProductWrite</c> (<c>catalog:read</c>/<c>catalog:write</c>).
/// </summary>
/// <remarks>
/// NOTA DE INTEGRAÇÃO: no momento em que este controller foi escrito, este worktree ainda não
/// tinha nenhuma implementação de US-010/US-011 (nem <c>ProductsController</c>/
/// <c>ProductVariantsController</c>, nem as policies <c>ProductRead</c>/<c>ProductWrite</c> em
/// <c>Program.cs</c> — só a camada <c>Nexora.Domain</c>/persistência de catálogo já existia). Os
/// atributos <c>[Authorize(Policy = "ProductRead"/"ProductWrite")]</c> abaixo assumem que essas
/// policies existirão após a mesclagem do trabalho paralelo de catálogo — sem elas registradas em
/// <c>Program.cs</c>, toda chamada a este controller falha em tempo de execução (não de
/// compilação). Ver relatório da tarefa para o que precisa ser plugado.
/// </remarks>
[ApiController]
[Authorize]
public sealed class PriceAdjustmentsController : ControllerBase
{
    private readonly ISender _sender;

    public PriceAdjustmentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Tabela de preço por canal de uma variante (US-014 §10) — os quatro canais, com herança do preço base já resolvida.</summary>
    [HttpGet("v1/catalog/variants/{id:guid}/prices")]
    [Authorize(Policy = "ProductRead")]
    [ProducesResponseType(typeof(VariantPriceTableResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPriceTable([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("variant.id", id);
        var result = await _sender.Send(new ListVariantPricesByChannelQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Define o preço vigente de um ou mais canais de uma variante na mesma chamada (US-014 §7) —
    /// fecha automaticamente o preço anterior de cada canal informado e cria uma nova linha.
    /// </summary>
    [HttpPut("v1/catalog/variants/{id:guid}/prices")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(VariantPriceTableResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetPriceTable(
        [FromRoute] Guid id,
        [FromBody] SetVariantChannelPriceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("variant.id", id);
        var result = await _sender.Send(new SetVariantChannelPriceCommand(id, request.Prices), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Reajuste percentual em massa (US-014 §7) — aplica o percentual sobre o preço efetivo de um
    /// canal para todas as variações ativas da categoria, em uma única transação.
    /// </summary>
    [HttpPost("v1/catalog/prices/bulk-adjust")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(BulkAdjustPricesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> BulkAdjust(
        [FromBody] BulkAdjustPricesRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("category.id", request.CategoryId);
        var result = await _sender.Send(
            new BulkAdjustPricesByCategoryCommand(request.CategoryId, request.Channel, request.Percent),
            cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
