using System.Diagnostics;
using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Devices.Commands.DeleteDevice;
using Nexora.Application.Devices.Commands.RenameDevice;
using Nexora.Application.Devices.Commands.RevokeDevice;
using Nexora.Application.Devices.Queries.ListDevices;
using Nexora.Contracts.Devices;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Gestão de dispositivos na nuvem — porta de
/// <c>apps/api-cloud/src/modules/devices/devices.controller.ts</c>. Só lista/renomeia/revoga:
/// geração de código de pareamento e o próprio pareamento só existem no edge (o
/// <c>CloudDevicesModule</c> original lança erro para os dois casos — pareamento precisa
/// funcionar offline, ADR-001).
/// </summary>
[ApiController]
[Authorize]
[Route("v1/devices")]
public sealed class DevicesController : ControllerBase
{
    private readonly ISender _sender;

    public DevicesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lista os dispositivos pareados no tenant (todas as lojas).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DeviceListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDevicesQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Renomeia o rótulo de apresentação de um dispositivo.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "DeviceManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(
        [FromRoute] Guid id,
        [FromBody] RenameDeviceRequest request,
        CancellationToken cancellationToken)
    {
        // ADR-022/US-005 §11: device.id no span mesmo se o comando falhar (ex.: 404 cross-tenant) —
        // o alvo já é conhecido pela rota, sem depender do resultado do comando.
        Activity.Current?.SetTag("device.id", id);
        var result = await _sender.Send(new RenameDeviceCommand(id, request.Label), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Revoga um dispositivo e encerra suas sessões ativas.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DeviceManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("device.id", id);
        var result = await _sender.Send(new RevokeDeviceCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Exclui (soft delete) um dispositivo já revogado da listagem. Rota <c>POST .../delete</c> em
    /// vez de <c>HttpDelete</c>: o verbo DELETE deste controller já está ocupado por
    /// <see cref="Revoke"/> (revogação, não exclusão) — manter <c>DELETE /{id}</c> com o
    /// comportamento existente evita quebrar o contrato já publicado.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [Authorize(Policy = "DeviceManage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("device.id", id);
        var result = await _sender.Send(new DeleteDeviceCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
