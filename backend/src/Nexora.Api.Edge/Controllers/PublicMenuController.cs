using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Catalog.Products.Queries.GetLocalPublicMenu;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Cardápio público servido pelo edge (US-021 §7) — gêmeo de
/// <c>Nexora.Api.Cloud.Controllers.PublicMenuController</c>, mas sem <c>host</c>: o edge é a
/// autoridade operacional de um único tenant (ADR-004), igual a <see cref="BrandingController"/>.
/// Consumido pelo <c>web-menu</c> depois de resolver a mesa por <see cref="PublicTableController"/>.
/// </summary>
[ApiController]
[Route("v1/public/menu")]
public sealed class PublicMenuController : ControllerBase
{
    private readonly ISender _sender;

    public PublicMenuController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicMenuResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromQuery] string? channel, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "public, max-age=30, s-maxage=30";
        var result = await _sender.Send(new GetLocalPublicMenuQuery(channel), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
