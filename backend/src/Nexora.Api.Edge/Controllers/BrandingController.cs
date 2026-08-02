using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Branding.Queries.GetLocalBranding;
using Nexora.Contracts.Branding;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Identidade visual (branding) da loja servida por este edge — US-003, gap "resolução de tenant
/// por host não funciona para web-pos/web-kds": <c>Nexora.Api.Cloud.Controllers.BrandingController
/// .PublicBranding</c> resolve o tenant pelo domínio público customizado
/// (<c>cardapio.donabetinha.com.br</c>), mas POS/KDS rodam na LAN da loja, onde o host HTTP nunca
/// bate com <c>Tenant.Domain</c>. Aqui não existe resolução por host: o edge é a autoridade
/// operacional de exatamente UM tenant (ADR-004, "uma loja = um tenant") e devolve sempre o
/// branding desse tenant, igual a como <c>PinScreen</c>/pareamento de dispositivo já funcionam
/// sem depender de host.
/// </summary>
[ApiController]
[Route("v1/local")]
public sealed class BrandingController : ControllerBase
{
    private readonly ISender _sender;

    public BrandingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Identidade visual do tenant desta instalação — usada por <c>web-pos</c>/<c>web-kds</c>
    /// antes mesmo do pareamento do dispositivo ou do login por PIN (mesmo nível de confiança de
    /// <c>AuthController.LoginWithPin</c>: a rede local da loja já é a fronteira de segurança).
    /// Sem parâmetro de host — propositalmente incompatível com o contrato de
    /// <c>GET /v1/public/branding?host=...</c> do cloud, para não sugerir que os dois algum dia
    /// aceitem o mesmo tipo de entrada.
    /// </summary>
    [HttpGet("branding")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BrandingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LocalBranding(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLocalBrandingQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
