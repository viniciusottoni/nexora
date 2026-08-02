using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.ProductModifierGroups.Commands.LinkModifierGroupToProduct;
using Nexora.Application.Catalog.ProductModifierGroups.Commands.UnlinkModifierGroupFromProduct;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Vínculo N:N entre produto e grupo de modificadores (US-012 — "reuso do mesmo grupo em vários
/// produtos"). Mesma nota de <see cref="ModifierGroupsController"/> sobre a permissão ser checada
/// no handler de Application em vez de uma <c>AuthorizationPolicy</c> nomeada.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/catalog/products/{productId:guid}/modifier-groups")]
public sealed class ProductModifierGroupsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductModifierGroupsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Vincula um grupo de modificadores já existente ao produto.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductModifierGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Link(
        [FromRoute] Guid productId, [FromBody] LinkModifierGroupToProductRequest request, CancellationToken cancellationToken)
    {
        var command = new LinkModifierGroupToProductCommand(productId, request.GroupId, request.SortOrder);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(Link), new { productId }, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Remove o vínculo entre o grupo e o produto (o grupo continua existindo para os demais produtos que o reusam).</summary>
    [HttpDelete("{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlink(
        [FromRoute] Guid productId, [FromRoute] Guid groupId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UnlinkModifierGroupFromProductCommand(productId, groupId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
