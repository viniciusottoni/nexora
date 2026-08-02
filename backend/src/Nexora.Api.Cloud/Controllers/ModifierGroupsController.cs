using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.ModifierGroups.Commands.CreateModifierGroup;
using Nexora.Application.Catalog.ModifierGroups.Commands.DeleteModifierGroup;
using Nexora.Application.Catalog.ModifierGroups.Commands.UpdateModifierGroup;
using Nexora.Application.Catalog.ModifierGroups.Queries.ListModifierGroups;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// CRUD de grupos de modificadores (US-012 — "Como gestor, quero definir adicionais, remoções e
/// opções obrigatórias por produto"). A permissão exigida (<c>catalog:read</c>/<c>catalog:write</c>,
/// <c>Nexora.Domain.Platform.PermissionCatalog</c>) é checada dentro de cada handler de
/// Application, não por uma <c>[Authorize(Policy=...)]</c> nomeada aqui — este controller nasceu
/// num worktree isolado, em paralelo com outros agentes trabalhando no mesmo <c>Program.cs</c>
/// compartilhado, e não pôde registrar uma policy nova lá (ver relatório da tarefa para a policy
/// "ModifierGroupRead"/"ModifierGroupWrite" recomendada como reforço em profundidade).
/// </summary>
[ApiController]
[Authorize]
[Route("v1/catalog/modifier-groups")]
public sealed class ModifierGroupsController : ControllerBase
{
    private readonly ISender _sender;

    public ModifierGroupsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista os grupos de modificadores do tenant autenticado, com modificadores e produtos vinculados.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ModifierGroupListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListModifierGroupsQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Cria um grupo de modificadores (ex.: "Tamanho", "Adicionais").</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ModifierGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateModifierGroupRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateModifierGroupCommand(
            request.Name, request.MinSelect, request.MaxSelect, request.IsRequired, request.SortOrder);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(List), null, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Atualiza a regra de seleção (mínimo/máximo) de um grupo já existente — reflete em todos os produtos que o reusam.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ModifierGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdateModifierGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateModifierGroupCommand(id, request.MinSelect, request.MaxSelect), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Remove um grupo de modificadores — cascateia soft delete para seus modificadores e desvincula de todo produto.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteModifierGroupCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
