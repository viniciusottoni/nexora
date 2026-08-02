using System.Security.Claims;
using System.Text.Encodings.Web;
using Nexora.Application.Installations.Commands.AuthenticateInstallationRequest;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Nexora.Api.Cloud.Infrastructure.Auth;

/// <summary>
/// Esquema de autenticação "Installation" — porta de <c>InstallationAuthGuard</c>
/// (<c>installation-auth.guard.ts</c>) para o pipeline padrão de autenticação do ASP.NET Core.
/// Registrar em <c>Program.cs</c>:
/// <code>
/// builder.Services.AddAuthentication()
///     .AddScheme&lt;AuthenticationSchemeOptions, InstallationAuthenticationHandler&gt;("Installation", null);
/// </code>
/// e usar <c>[Authorize(AuthenticationSchemes = "Installation")]</c> nas rotas que hoje têm o
/// TODO "trocar por [Authorize(...)] quando o módulo de instalação publicar seu
/// AuthenticationHandler" (ex.: <c>BackupsController</c>) — é este handler.
/// <para>
/// Em caso de sucesso, popula claims <c>tid</c>/<c>sid</c> (mesmas lidas por
/// <c>CloudCurrentTenantContext</c>) — o que também resolve, de forma padrão e sem gambiarra,
/// o contexto de tenant para o interceptor de conexão do EF Core (ADR-004/RLS) nas rotas
/// autenticadas por instalação, sem precisar de nenhum código condicional por tipo de esquema.
/// </para>
/// </summary>
public sealed class InstallationAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ISender _sender;

    public InstallationAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISender sender)
        : base(options, logger, encoder)
    {
        _sender = sender;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!TryReadHeader("X-Installation-Id", out var installationIdRaw) ||
            !Guid.TryParse(installationIdRaw, out var installationId) ||
            !TryReadHeader("X-Installation-Timestamp", out var timestamp) ||
            !TryReadHeader("X-Installation-Nonce", out var nonce) ||
            !TryReadHeader("X-Installation-Signature", out var signature))
        {
            return AuthenticateResult.NoResult();
        }

        var command = new AuthenticateInstallationRequestCommand(
            installationId,
            timestamp,
            nonce,
            signature,
            Request.Method,
            $"{Request.Path}{Request.QueryString}");

        var result = await _sender.Send(command, Context.RequestAborted);

        if (result.IsFailure || result.Value is null)
        {
            return AuthenticateResult.Fail("Não foi possível autenticar a requisição da instalação.");
        }

        var claims = new[]
        {
            new Claim("tid", result.Value.TenantId.ToString()),
            new Claim("sid", result.Value.StoreId.ToString()),
            new Claim("installation_id", result.Value.InstallationId.ToString()),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private bool TryReadHeader(string name, out string value)
    {
        if (Request.Headers.TryGetValue(name, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            value = raw.ToString();
            return true;
        }

        value = string.Empty;
        return false;
    }
}
