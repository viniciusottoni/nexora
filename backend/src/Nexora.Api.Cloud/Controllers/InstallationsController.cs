using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Installations.Commands.ConsumeInstallationToken;
using Nexora.Application.Installations.Commands.RegisterInstallation;
using Nexora.Application.Installations.Queries.GetInitialSyncPage;
using Nexora.Application.Installations.Queries.GetInstallationSyncHealth;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Installations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Registro de instalação edge e sincronização inicial de loja nova — porta de
/// <c>installations.controller.ts</c> (registro/consumo de token) e
/// <c>initial-sync.controller.ts</c> (pull/health). Dois esquemas de autenticação diferentes na
/// mesma classe, por isso as rotas usam template absoluto por ação em vez de
/// <c>[Route]</c> de classe: <c>register</c>/<c>consume-token</c> são públicas (o dispositivo
/// ainda não tem nenhuma credencial); <c>pull</c>/<c>health</c> exigem o esquema "Installation"
/// (<see cref="Infrastructure.Auth.InstallationAuthenticationHandler"/>).
/// </summary>
[ApiController]
public sealed class InstallationsController : ControllerBase
{
    private readonly ISender _sender;

    public InstallationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Consome o token de instalação emitido no provisionamento do tenant (elo Tenants → Installations).</summary>
    [AllowAnonymous]
    [HttpPost("v1/platform/installations/consume-token")]
    [ProducesResponseType(typeof(ConsumeInstallationTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConsumeToken(
        [FromHeader(Name = "X-Install-Token")] string? installToken,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ConsumeInstallationTokenCommand(installToken ?? string.Empty), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Registra a instalação física do edge (chave pública gerada localmente) — idempotente para o mesmo par instalação/chave.</summary>
    [AllowAnonymous]
    [HttpPost("v1/platform/installations/register")]
    [ProducesResponseType(typeof(RegisterInstallationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Register(
        [FromHeader(Name = "X-Install-Token")] string? installToken,
        [FromBody] RegisterInstallationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterInstallationCommand(
            installToken ?? string.Empty, request.InstallationId, request.Hostname, request.Version, request.PublicKey);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>Página da sincronização inicial (catálogo + autorização + configuração) para uma loja nova.</summary>
    [Authorize(AuthenticationSchemes = "Installation")]
    [HttpGet("v1/sync/pull")]
    [ProducesResponseType(typeof(InitialSyncPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pull(
        [FromQuery] int cursor,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var query = new GetInitialSyncPageQuery(CurrentTenantId, CurrentStoreId, CurrentInstallationId, cursor, limit ?? 500);
        var result = await _sender.Send(query, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Versão esperada e versão de configuração corrente — usado pelo edge para saber se precisa atualizar.</summary>
    [Authorize(AuthenticationSchemes = "Installation")]
    [HttpGet("v1/sync/health")]
    [ProducesResponseType(typeof(InstallationSyncHealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetInstallationSyncHealthQuery(CurrentTenantId, CurrentInstallationId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    private Guid CurrentTenantId => ReadGuidClaim("tid");

    private Guid CurrentStoreId => ReadGuidClaim("sid");

    private Guid CurrentInstallationId => ReadGuidClaim("installation_id");

    private Guid ReadGuidClaim(string type) =>
        Guid.TryParse(User.FindFirst(type)?.Value, out var value) ? value : Guid.Empty;
}
