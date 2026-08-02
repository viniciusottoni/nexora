using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Roles.Commands.CreateRole;
using Nexora.Application.Roles.Commands.UpdateRole;
using Nexora.Application.Roles.Queries.ListRoles;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Catálogo de permissões e CRUD de papéis (roles) customizados por tenant (ADR-023).
/// Porta de <c>roles.controller.ts</c>. Exige permissão <c>user:read</c> (listar) e
/// <c>user:write</c> (criar/editar) — mesmo código que a versão TS original exigia (ver policies
/// "RoleRead"/"RoleWrite" em Program.cs, US-004 gap "RBAC não verifica permissão em nenhum
/// endpoint de negócio"; antes desta correção o controller só tinha <c>[Authorize]</c> genérico,
/// então qualquer usuário autenticado do tenant conseguia listar e reescrever papéis/permissões).
/// </summary>
[ApiController]
[Authorize]
[Route("v1/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly ISender _sender;

    public RolesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista os papéis do tenant autenticado e o catálogo de permissões.</summary>
    [HttpGet]
    [Authorize(Policy = "RoleRead")]
    [ProducesResponseType(typeof(RoleListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListRolesQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Cria um papel customizado no tenant autenticado.</summary>
    [HttpPost]
    [Authorize(Policy = "RoleWrite")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(request.Code, request.Name, request.Permissions ?? Array.Empty<string>());
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(List), null, result.Value);

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Atualiza nome e/ou permissões de um papel do tenant autenticado.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "RoleWrite")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoleCommand(id, request.Name, request.Permissions);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
