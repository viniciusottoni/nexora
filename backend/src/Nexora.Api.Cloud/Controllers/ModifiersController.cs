using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.Modifiers.Commands.CreateModifier;
using Nexora.Application.Catalog.Modifiers.Commands.MarkModifierAvailable;
using Nexora.Application.Catalog.Modifiers.Commands.MarkModifierUnavailable;
using Nexora.Application.Catalog.Modifiers.Commands.UpdateModifier;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// CRUD de modificadores (opções) dentro de um grupo (US-012). Mesma nota de
/// <see cref="ModifierGroupsController"/> sobre a permissão ser checada no handler de Application
/// em vez de uma <c>AuthorizationPolicy</c> nomeada — este controller não pôde tocar
/// <c>Program.cs</c> (worktree isolado em paralelo com outros agentes).
/// </summary>
[ApiController]
[Authorize]
[Route("v1/catalog/modifier-groups/{groupId:guid}/modifiers")]
public sealed class ModifiersController : ControllerBase
{
    private readonly ISender _sender;

    public ModifiersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Cria um modificador (ex.: "Borda Catupiry", "Sem cebola") dentro do grupo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ModifierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid groupId, [FromBody] CreateModifierRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateModifierCommand(
            groupId, request.Name, request.PriceDelta, request.IngredientId, request.Quantity, request.SortOrder);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(Create), new { groupId }, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Atualiza o <c>price_delta</c> de um modificador — soma ao preço do item quando positivo, sem custo quando zero.</summary>
    [HttpPatch("{modifierId:guid}")]
    [ProducesResponseType(typeof(ModifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid groupId, [FromRoute] Guid modifierId, [FromBody] UpdateModifierRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateModifierCommand(groupId, modifierId, request.PriceDelta), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Marca disponibilidade do modificador (ex.: insumo em falta) — não remove o cadastro, só oculta a opção no cardápio.</summary>
    [HttpPatch("{modifierId:guid}/availability")]
    [ProducesResponseType(typeof(ModifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAvailability(
        [FromRoute] Guid groupId, [FromRoute] Guid modifierId, [FromBody] UpdateModifierAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = request.IsAvailable
            ? await _sender.Send(new MarkModifierAvailableCommand(groupId, modifierId), cancellationToken)
            : await _sender.Send(new MarkModifierUnavailableCommand(groupId, modifierId), cancellationToken);

        return result.ToActionResult(HttpContext);
    }
}
