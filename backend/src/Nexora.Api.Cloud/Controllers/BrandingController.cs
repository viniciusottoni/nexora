using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Branding.Commands.PrepareBrandingUpload;
using Nexora.Application.Branding.Commands.UpdateBranding;
using Nexora.Application.Branding.Queries.GetBrandingManifest;
using Nexora.Application.Branding.Queries.GetLocalBranding;
using Nexora.Application.Branding.Queries.GetPublicBranding;
using Nexora.Contracts.Branding;
using Nexora.Contracts.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Identidade visual (branding) por tenant — cores, logos, textos e PWA manifest. Toda a
/// diferença é configuração por tenant (ADR-013): nunca um "if" de estabelecimento, sempre um
/// campo em <c>TenantConfig.Branding</c>. Porta de <c>branding.controller.ts</c>.
/// Escrita (<see cref="Update"/>/<see cref="PrepareUpload"/>) exige a policy <c>ConfigWrite</c>
/// (permissão <c>config:write</c> do catálogo, US-003 — antes só <c>[Authorize]</c> genérico
/// deixava qualquer usuário autenticado do tenant alterar a marca pública do estabelecimento).
/// Leitura pública continua sem autenticação, como já era (RN-016).
/// </summary>
[ApiController]
[Route("v1")]
public sealed class BrandingController : ControllerBase
{
    private readonly ISender _sender;

    public BrandingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Identidade visual pública de um tenant, resolvida pelo domínio customizado — usada pelo frontend antes do login.</summary>
    [HttpGet("public/branding")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BrandingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublicBranding([FromQuery] string host, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "public, max-age=60, s-maxage=60";
        var result = await _sender.Send(new GetPublicBrandingQuery(host), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Web App Manifest do tenant resolvido pelo domínio customizado.</summary>
    [HttpGet("tenant/branding.webmanifest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Manifest([FromQuery] string host, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "public, max-age=60, s-maxage=60";
        Response.Headers.ContentType = "application/manifest+json; charset=utf-8";
        var result = await _sender.Send(new GetBrandingManifestQuery(host), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Identidade visual do tenant autenticado, para prefill da tela de administração de marca
    /// (US-003, gap "não existe tela de administração de marca" — <c>apps/web-admin</c> precisa de
    /// algum jeito de carregar o valor atual antes de editar; não existia nenhum GET autenticado,
    /// só o público por host). Reaproveita <see cref="GetLocalBrandingQuery"/> — a mesma consulta
    /// que <c>Nexora.Api.Edge.Controllers.BrandingController</c> usa para POS/KDS: o tenant vem de
    /// <c>ICurrentTenantContext.TenantId</c> (aqui, da claim "tid" do JWT), não de host, então o
    /// handler é idêntico nas duas APIs.
    /// </summary>
    [HttpGet("tenant/branding")]
    [Authorize]
    [ProducesResponseType(typeof(BrandingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OwnBranding(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLocalBrandingQuery(), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Atualiza (patch parcial) a identidade visual do tenant autenticado.</summary>
    [HttpPatch("tenant/branding")]
    [Authorize(Policy = "ConfigWrite")]
    [ProducesResponseType(typeof(UpdateBrandingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateBrandingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateBrandingCommand(request), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Prepara uma URL de upload direto (pré-assinada) para um novo logo/favicon/ícone.</summary>
    [HttpPost("tenant/branding/logo")]
    [Authorize(Policy = "ConfigWrite")]
    [ProducesResponseType(typeof(UploadBrandingAssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PrepareUpload(
        [FromBody] UploadBrandingAssetRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new PrepareBrandingUploadCommand(request.Kind, request.ContentType, request.Bytes, request.Sha256);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
