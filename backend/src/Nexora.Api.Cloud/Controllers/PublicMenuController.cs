using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.Products.Queries.GetPublicMenu;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Cardápio público de um estabelecimento (US-010 §7) — usado pelo cardápio da mesa/PWA/delivery
/// antes de qualquer login, mesmo espírito de <see cref="BrandingController.PublicBranding"/>.
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

    /// <summary>Categorias e produtos ativos de um estabelecimento, resolvido pelo domínio customizado.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicMenuResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromQuery] string host, [FromQuery] string? channel, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "public, max-age=30, s-maxage=30";
        var result = await _sender.Send(new GetPublicMenuQuery(host, channel), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
