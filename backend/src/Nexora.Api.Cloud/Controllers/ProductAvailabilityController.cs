using System.Diagnostics;
using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.Availability.Commands.MarkProductAvailable;
using Nexora.Application.Catalog.Availability.Commands.MarkProductUnavailable;
using Nexora.Application.Catalog.Availability.Queries.ListUnavailableProducts;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Indisponibilidade operacional de produto (US-015, RF-CAT-07) — distinto de
/// <see cref="ProductsController.Activate"/>/<see cref="ProductsController.Deactivate"/> (US-010,
/// ativação/desativação de cadastro). Mesma família de permissão de <see cref="ProductsController"/>
/// (<c>ProductRead</c>/<c>ProductWrite</c>, recurso <c>catalog</c> do catálogo de permissões) — a
/// história pede para reaproveitar a policy já existente, não criar uma nova.
///
/// <para>
/// [DESVIO DOCUMENTADO] O doc da US-015 (§7) descreve o corpo de exemplo como
/// <c>POST /v1/catalog/variants/:id/availability</c> (granularidade de VARIANTE). O domínio já
/// implementado por US-010/US-011 (<c>Nexora.Domain.Catalog.Product.IsAvailable</c>/
/// <c>MarkUnavailable</c>/<c>MarkAvailable</c>) modela disponibilidade no nível de PRODUTO, não de
/// <c>ProductVariant</c> — não é possível mudar isso sem alterar `Domain`, fora do escopo permitido
/// desta tarefa. Por isso a rota real é <c>POST /v1/catalog/products/:id/availability</c> (produto),
/// como aliás já orienta o enunciado desta tarefa. Reportado no relatório final.
/// </para>
/// </summary>
[ApiController]
[Authorize]
public sealed class ProductAvailabilityController : ControllerBase
{
    private readonly ISender _sender;

    public ProductAvailabilityController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Marca um produto (in)disponível. <see cref="SetProductAvailabilityRequest.IsAvailable"/>
    /// <c>false</c> exige <see cref="SetProductAvailabilityRequest.Reason"/> (US-015 §3.1: "marcação
    /// de indisponibilidade por variação, com motivo"); <c>true</c> devolve o produto ao cardápio.
    /// </summary>
    [HttpPost("v1/catalog/products/{id:guid}/availability")]
    [Authorize(Policy = "ProductAvailability")]
    [ProducesResponseType(typeof(ProductAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAvailability(
        [FromRoute] Guid id,
        [FromBody] SetProductAvailabilityRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("product.id", id);

        if (request.IsAvailable)
        {
            var availableResult = await _sender.Send(new MarkProductAvailableCommand(id), cancellationToken);
            return availableResult.ToActionResult(HttpContext);
        }

        var unavailableResult = await _sender.Send(
            new MarkProductUnavailableCommand(id, request.Reason ?? string.Empty, request.AutoRestoreNextDay),
            cancellationToken);
        return unavailableResult.ToActionResult(HttpContext);
    }

    /// <summary>Lista os produtos indisponíveis do tenant — "lista sempre visível ao gestor" (US-015 §10).</summary>
    [HttpGet("v1/catalog/products/unavailable")]
    [Authorize(Policy = "ProductAvailability")]
    [ProducesResponseType(typeof(UnavailableProductsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUnavailable(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListUnavailableProductsQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
