using System.Diagnostics;
using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.Products.Commands.ActivateProduct;
using Nexora.Application.Catalog.Products.Commands.ConfirmProductImage;
using Nexora.Application.Catalog.Products.Commands.CreateProduct;
using Nexora.Application.Catalog.Products.Commands.DeactivateProduct;
using Nexora.Application.Catalog.Products.Commands.PrepareProductImageUpload;
using Nexora.Application.Catalog.Products.Commands.ReorderProducts;
using Nexora.Application.Catalog.Products.Commands.UpdateProduct;
using Nexora.Application.Catalog.Products.Queries.ListProducts;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// CRUD de produtos do cardápio (US-010) — cardápio é editado na nuvem e apenas lido no local
/// (CLAUDE.md). Exige permissão <c>catalog:read</c> (listar) e <c>catalog:write</c>
/// (criar/editar/reordenar/ativar/desativar/upload de foto), mesmo catálogo de permissões de
/// <see cref="StationsController"/>/<see cref="CategoriesController"/>.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/catalog/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista os produtos do tenant autenticado (ativos e inativos), opcionalmente filtrados por categoria.</summary>
    [HttpGet]
    [Authorize(Policy = "ProductRead")]
    [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListProductsQuery(categoryId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Cria um produto do cardápio, vinculado a uma categoria e opcionalmente a uma praça de produção.</summary>
    [HttpPost]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.CategoryId,
            request.Name,
            request.StationId,
            request.Description,
            request.IngredientsText,
            request.Allergens,
            request.AllowsFractions,
            request.MaxFractions,
            request.Position,
            request.IsActive);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(List), null, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Reordena (arrastar e soltar) os produtos dentro de uma categoria.</summary>
    [HttpPatch("reorder")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reorder(
        [FromBody] ReorderProductsRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReorderProductsCommand(request.CategoryId, request.Order), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Atualiza campos de cadastro de um produto (nome, categoria, praça, descrição, ingredientes, alérgenos, fracionamento, posição).</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("product.id", id);
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.CategoryId,
            request.StationId,
            request.Description,
            request.IngredientsText,
            request.Allergens,
            request.AllowsFractions,
            request.MaxFractions,
            request.Position);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Ativa um produto previamente desativado, voltando a exibi-lo nos canais de venda.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        [FromRoute] Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("product.id", id);
        var result = await _sender.Send(new ActivateProductCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Desativa (nunca exclui fisicamente) um produto — distinto de indisponibilidade operacional (US-015).</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        [FromRoute] Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("product.id", id);
        var result = await _sender.Send(new DeactivateProductCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Prepara uma URL de upload direto (pré-assinada) para a foto de um produto.</summary>
    [HttpPost("{id:guid}/image")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(PrepareProductImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PrepareImageUpload(
        [FromRoute] Guid id,
        [FromBody] PrepareProductImageUploadRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("product.id", id);
        var command = new PrepareProductImageUploadCommand(id, request.ContentType, request.Bytes, request.Sha256);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Confirma que o upload direto da foto terminou com sucesso e registra o <c>MediaAsset</c> definitivo.</summary>
    [HttpPost("{id:guid}/image/confirm")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(typeof(ProductImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmImage(
        [FromRoute] Guid id,
        [FromBody] ConfirmProductImageRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("product.id", id);
        var command = new ConfirmProductImageCommand(id, request.Url, request.ContentType, request.Bytes, request.Sha256, request.Width, request.Height);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
