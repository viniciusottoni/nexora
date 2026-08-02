using System.Diagnostics;
using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.Categories.Commands.CreateCategory;
using Nexora.Application.Catalog.Categories.Commands.DeactivateCategory;
using Nexora.Application.Catalog.Categories.Commands.ReorderCategories;
using Nexora.Application.Catalog.Categories.Commands.UpdateCategory;
using Nexora.Application.Catalog.Categories.Queries.ListCategories;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// CRUD de categorias do cardápio (US-010) — cardápio é editado na nuvem e apenas lido no local
/// (CLAUDE.md). Exige permissão <c>catalog:read</c> (listar) e <c>catalog:write</c>
/// (criar/editar/reordenar/desativar), mesmo catálogo de permissões de
/// <see cref="StationsController"/>.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/catalog/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista todas as categorias do tenant autenticado (ativas e inativas), com a contagem de produtos vinculados.</summary>
    [HttpGet]
    [Authorize(Policy = "CategoryRead")]
    [ProducesResponseType(typeof(CategoryListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListCategoriesQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Cria uma categoria do cardápio.</summary>
    [HttpPost]
    [Authorize(Policy = "CategoryWrite")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(request.Name, request.Description, request.Position);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(List), null, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Reordena (arrastar e soltar) as categorias do cardápio.</summary>
    [HttpPatch("reorder")]
    [Authorize(Policy = "CategoryWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reorder(
        [FromBody] ReorderCategoriesRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReorderCategoriesCommand(request.Order), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Atualiza campos de uma categoria — inclusive reativá-la (<c>isActive: true</c>).</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "CategoryWrite")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("category.id", id);
        var command = new UpdateCategoryCommand(id, request.Name, request.Description, request.Position, request.IsActive);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Desativa (nunca exclui fisicamente) uma categoria — some dos canais de venda, produtos vinculados continuam intactos.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CategoryWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        [FromRoute] Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("category.id", id);
        var result = await _sender.Send(new DeactivateCategoryCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
