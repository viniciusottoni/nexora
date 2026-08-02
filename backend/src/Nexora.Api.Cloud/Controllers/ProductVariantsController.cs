using System.Diagnostics;
using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.Prices.Commands.SetVariantPrice;
using Nexora.Application.Catalog.Variants.Commands.ActivateVariant;
using Nexora.Application.Catalog.Variants.Commands.CreateVariant;
using Nexora.Application.Catalog.Variants.Commands.DeactivateVariant;
using Nexora.Application.Catalog.Variants.Commands.MarkVariantAsDefault;
using Nexora.Application.Catalog.Variants.Commands.UpdateVariant;
using Nexora.Application.Catalog.Variants.Queries.GetVariant;
using Nexora.Application.Catalog.Variants.Queries.ListVariantsForProduct;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Variações de produto com preço próprio (US-011) — a <c>product_variant</c> é a unidade real de
/// venda/preço, não o <c>Product</c> (US-010). Mesma família de permissão de
/// <see cref="ProductsController"/> (<c>catalog:read</c>/<c>catalog:write</c>), reaproveitando as
/// policies <c>ProductRead</c>/<c>ProductWrite</c> já registradas em Program.cs — variantes e
/// preços são um sub-recurso de produto, não um recurso novo do catálogo de permissões.
/// </summary>
[ApiController]
[Authorize]
public sealed class ProductVariantsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductVariantsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista as variantes de um produto (ativas e inativas) com o preço vigente de cada uma no canal consultado (padrão <c>DineIn</c>).</summary>
    [HttpGet("v1/catalog/products/{productId:guid}/variants")]
    [Authorize(Policy = "ProductRead")]
    [ProducesResponseType(typeof(VariantListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListForProduct(
        [FromRoute] Guid productId,
        [FromQuery] string? channel,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListVariantsForProductQuery(productId, channel), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Cria uma variante (tamanho) do produto, com o preço base em um único canal (padrão <c>DineIn</c>).</summary>
    [HttpPost("v1/catalog/products/{productId:guid}/variants")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid productId,
        [FromBody] CreateVariantRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("product.id", productId);
        var command = new CreateVariantCommand(
            productId,
            request.Name,
            request.SizeCode,
            request.Sku,
            request.PrepMinutes,
            request.IsDefault,
            request.BasePrice,
            request.Channel);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Consulta uma variante pelo id, com o preço vigente no canal consultado (padrão <c>DineIn</c>).</summary>
    [HttpGet("v1/catalog/variants/{id:guid}")]
    [Authorize(Policy = "ProductRead")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id, [FromQuery] string? channel, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetVariantQuery(id, channel), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Atualiza nome, SKU e <c>sizeCode</c> de uma variante.</summary>
    [HttpPatch("v1/catalog/variants/{id:guid}")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateVariantRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("variant.id", id);
        var result = await _sender.Send(new UpdateVariantCommand(id, request.Name, request.SizeCode, request.Sku), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Ativa uma variante previamente desativada.</summary>
    [HttpPost("v1/catalog/variants/{id:guid}/activate")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        [FromRoute] Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("variant.id", id);
        var result = await _sender.Send(new ActivateVariantCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Desativa (nunca exclui fisicamente) uma variante — não existe endpoint de exclusão física
    /// (US-011 §3.1/§12, cenário "Exclusão com histórico"): desativar é a única forma de remoção.
    /// </summary>
    [HttpPost("v1/catalog/variants/{id:guid}/deactivate")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        [FromRoute] Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("variant.id", id);
        var result = await _sender.Send(new DeactivateVariantCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Marca a variante como padrão do produto, desmarcando qualquer outra variante padrão do mesmo produto.</summary>
    [HttpPost("v1/catalog/variants/{id:guid}/mark-default")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsDefault(
        [FromRoute] Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("variant.id", id);
        var result = await _sender.Send(new MarkVariantAsDefaultCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Define o preço vigente da variante em um canal (padrão <c>DineIn</c>), fechando
    /// automaticamente o preço anterior do mesmo canal (histórico preservado, US-011 §4).
    /// </summary>
    [HttpPost("v1/catalog/variants/{id:guid}/prices")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(PriceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrice(
        [FromRoute] Guid id,
        [FromBody] SetVariantPriceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("variant.id", id);
        var result = await _sender.Send(new SetVariantPriceCommand(id, request.Amount, request.Channel), cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(Get), new { id }, result.Value);

        return result.ToActionResult(HttpContext);
    }
}
