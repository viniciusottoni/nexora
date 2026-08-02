using System.Diagnostics;
using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Catalog.Availability.Commands.MarkProductAvailable;
using Nexora.Application.Catalog.Availability.Commands.MarkProductUnavailable;
using Nexora.Application.Catalog.Availability.Queries.ListUnavailableProducts;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Indisponibilidade operacional de produto a partir do KDS/edge (US-015, RF-CAT-07) — "a cozinha
/// marca no local" (autoridade bidirecional desta história). Mesmas policies
/// <c>ProductRead</c>/<c>ProductWrite</c> do gêmeo em <c>Nexora.Api.Cloud</c> (recurso <c>catalog</c>
/// do catálogo de permissões, <c>Nexora.Domain.Platform.PermissionCatalog</c>) — registradas aqui
/// pela primeira vez em <c>Program.cs</c> do Api.Edge, que antes desta história não expunha
/// nenhum endpoint de catálogo (US-010/US-011 são cardápio "editado na nuvem, só lido no local" —
/// esta é a exceção bidirecional citada no cabeçalho do doc da US-015).
///
/// <para>
/// [DESVIO DOCUMENTADO] O doc da US-015 (§7) usa <c>{variantId}</c> no path — o domínio já
/// implementado (US-010/US-011) modela disponibilidade no nível de <c>Product</c>, não de
/// <c>ProductVariant</c> (ver nota equivalente em <c>Nexora.Api.Cloud.Controllers.ProductAvailabilityController</c>).
/// O parâmetro de rota abaixo é o id do PRODUTO, apesar do path começar com <c>/kds/products/</c>
/// (que já bate com o texto do doc — só o NOME do segmento variável muda de "variantId" para "id
/// de produto"). Reportado no relatório final.
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
    /// Marca um produto indisponível a partir do KDS — "a marcação precisa caber em um toque"
    /// (US-015 §10; detalhamento fino da interação é US-044). Funciona com a loja sem internet
    /// (US-015 §9): grava local e propaga pelo <c>CatalogAvailabilityHub</c> na LAN; a nuvem só
    /// recebe na próxima sincronização (ver <see cref="Application.Abstractions.Realtime.IAvailabilityBroadcaster"/>).
    /// </summary>
    [HttpPost("v1/kds/products/{id:guid}/unavailable")]
    [Authorize(Policy = "ProductAvailability")]
    [ProducesResponseType(typeof(ProductAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkUnavailable(
        [FromRoute] Guid id,
        [FromBody] MarkProductUnavailableRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("product.id", id);
        var result = await _sender.Send(
            new MarkProductUnavailableCommand(id, request.Reason, request.AutoRestoreNextDay), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Retorno manual à disponibilidade a partir do KDS.</summary>
    [HttpPost("v1/kds/products/{id:guid}/available")]
    [Authorize(Policy = "ProductAvailability")]
    [ProducesResponseType(typeof(ProductAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAvailable(
        [FromRoute] Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("product.id", id);
        var result = await _sender.Send(new MarkProductAvailableCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Lista os produtos indisponíveis — "lista sempre visível ao garçom, no topo do cardápio" (US-015 §10).</summary>
    [HttpGet("v1/kds/products/unavailable")]
    [Authorize(Policy = "ProductAvailability")]
    [ProducesResponseType(typeof(UnavailableProductsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUnavailable(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListUnavailableProductsQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
