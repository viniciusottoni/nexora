using System.Diagnostics;
using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Devices.Commands.CreatePairingCode;
using Nexora.Application.Devices.Commands.DeleteDevice;
using Nexora.Application.Devices.Commands.PairDevice;
using Nexora.Application.Devices.Commands.RenameDevice;
using Nexora.Application.Devices.Commands.RevokeDevice;
using Nexora.Application.Devices.Commands.UpdateDevicePreferences;
using Nexora.Application.Devices.Queries.ListDevices;
using Nexora.Contracts.Devices;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Pareamento e gestão de dispositivos no edge — porta de
/// <c>apps/api-edge/src/modules/devices/devices.controller.ts</c>. Diferente do
/// <c>Api.Cloud</c>: aqui o edge também EMITE o código de pareamento e CONSOME esse código
/// (<see cref="Pair"/>), porque o pareamento precisa funcionar mesmo sem internet (ADR-001,
/// local-first) — a nuvem só lista/renomeia/revoga (ver <c>CloudDevicesModule</c> original,
/// que lança erro para os dois casos).
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

    /// <summary>Gera um código de pareamento de uso único (10 min) para um novo dispositivo.</summary>
    [HttpPost("pairing-codes")]
    [Authorize(Policy = "DeviceManage")]
    [ProducesResponseType(typeof(CreatePairingCodeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePairingCode(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreatePairingCodeCommand(), cancellationToken);

        // 201 Created (contrato §7 da US-005) — não 200: a operação cria um recurso novo (o
        // código de pareamento). Sem CreatedAtAction: não há endpoint de leitura de um código de
        // pareamento individual (ele não é "recuperável" — de uso único e efêmero por design), daí
        // StatusCode direto em vez do padrão Created/CreatedAtAction usado em TenantsController/
        // RolesController, que apontam Location para um GET existente.
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Consome o código de pareamento e registra o dispositivo. Rota pública (sem JWT) — o
    /// dispositivo novo ainda não tem credencial nenhuma; tenant/loja vêm da identidade fixa
    /// do próprio servidor edge, não do chamador (ADR-004). Idempotency-Key continua
    /// obrigatório por ser POST (ADR-020).
    /// </summary>
    [HttpPost("pair")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PairDeviceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Pair(
        [FromBody] PairDeviceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new PairDeviceCommand(request.Code, request.Label, request.Kind, request.Fingerprint);
        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToActionResult(HttpContext);
        }

        // ADR-022/US-005 §11: device.id como atributo de span em toda requisição — aqui a
        // requisição É a própria criação do dispositivo (rota anônima, sem JWT, então
        // ActivityEnrichmentMiddleware não tem nenhum ICurrentTenantContext.DeviceId para
        // anexar). Sem isto, o pareamento — evento que a US pede para ser rastreável por
        // dispositivo — não carregaria device.id em nenhum span.
        Activity.Current?.SetTag("device.id", result.Value!.Device.Id);

        // 201 Created — mesmo raciocínio do CreatePairingCode: não há GET de dispositivo único
        // (só listagem), então Location não tem destino útil; StatusCode direto evita um
        // CreatedAtAction(nameof(List)) com Location que não resolveria o recurso criado.
        return StatusCode(StatusCodes.Status201Created, result.Value);
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
        // Tagueia ANTES de despachar o comando — o alvo da ação é conhecido pela rota, e a US
        // pede device.id no span mesmo quando o comando falha (ex.: 404 de dispositivo de outro
        // tenant), não só no caminho de sucesso.
        Activity.Current?.SetTag("device.id", id);
        var result = await _sender.Send(new RenameDeviceCommand(id, request.Label), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-042/US-045/US-047 — grava preferência "por dispositivo" do KDS (praça filtrada, som,
    /// modo pico), mesclando com o que já existe. Sem <c>[Authorize(Policy=...)]</c> de escrita
    /// aqui: a regra "autoatendimento sempre permitido, dispositivo alheio exige device:manage"
    /// mora no Handler (ver sua docstring) — só <c>[Authorize]</c> do controller (linha 26) já
    /// garante um tenant autenticado.
    /// </summary>
    [HttpPatch("{id:guid}/preferences")]
    [ProducesResponseType(typeof(DevicePreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences(
        [FromRoute] Guid id,
        [FromBody] UpdateDevicePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("device.id", id);
        var result = await _sender.Send(
            new UpdateDevicePreferencesCommand(id, request.Preferences.GetRawText()),
            cancellationToken);
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
