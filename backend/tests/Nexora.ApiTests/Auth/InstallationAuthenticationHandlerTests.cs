extern alias ApiCloud;
using System.Globalization;
using System.Text.Encodings.Web;
using ApiCloud::Nexora.Api.Cloud.Infrastructure.Auth;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Installations.Commands.AuthenticateInstallationRequest;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Nexora.ApiTests.Auth;

/// <summary>
/// Cobre o Gap P0-2 (US-006): <c>BackupsController</c> passou a exigir o esquema de autenticação
/// "Installation" (<see cref="InstallationAuthenticationHandler"/>) em vez de ler
/// <c>X-Installation-Id</c> manualmente. Estes testes provam, sem precisar de banco nem de host
/// HTTP completo, que (a) uma requisição sem a assinatura Ed25519 esperada nunca autentica, e
/// (b) uma requisição autenticada com sucesso produz exatamente a claim <c>installation_id</c>
/// que <see cref="BackupsController"/> lê para popular <c>AuthenticatedInstallationId</c> — o elo
/// entre o handler de autenticação e o controller que este gap corrigiu.
/// </summary>
public sealed class InstallationAuthenticationHandlerTests
{
    private static readonly AuthenticationScheme Scheme =
        new("Installation", "Installation", typeof(InstallationAuthenticationHandler));

    [Fact]
    public async Task Sem_Cabecalhos_De_Assinatura_Nao_Autentica_E_Nao_Chama_O_Sender()
    {
        var sender = Substitute.For<ISender>();
        var handler = await CreateHandlerAsync(sender, headers: null);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        await sender.DidNotReceive().Send(Arg.Any<AuthenticateInstallationRequestCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assinatura_Rejeitada_Pelo_Command_Handler_Resulta_Em_Falha_De_Autenticacao()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<AuthenticateInstallationRequestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<InstallationAuthContext>.Failure("Não foi possível autenticar.", "INSTALLATION_SIGNATURE_INVALID_CREDENTIALS")));

        var handler = await CreateHandlerAsync(sender, headers: ValidHeaders(Guid.NewGuid()));

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Assinatura_Valida_Autentica_E_Popula_Claim_Installation_Id_Lida_Pelo_BackupsController()
    {
        var installationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<AuthenticateInstallationRequestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<InstallationAuthContext>.Success(
                new InstallationAuthContext(tenantId, storeId, installationId))));

        var handler = await CreateHandlerAsync(sender, headers: ValidHeaders(installationId));

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirst("tid")!.Value.Should().Be(tenantId.ToString());
        result.Principal.FindFirst("sid")!.Value.Should().Be(storeId.ToString());

        // Mesma leitura de claim feita por BackupsController.AuthenticatedInstallationId — se o
        // nome da claim aqui e lá divergir, o backup upload autentica mas resolve instalação
        // errada (falha silenciosa, não um 401 óbvio).
        result.Principal.FindFirst("installation_id")!.Value.Should().Be(installationId.ToString());
    }

    private static Dictionary<string, string> ValidHeaders(Guid installationId) => new()
    {
        ["X-Installation-Id"] = installationId.ToString(),
        ["X-Installation-Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
        ["X-Installation-Nonce"] = Guid.NewGuid().ToString("N"),
        ["X-Installation-Signature"] = "dGVzdC1zaWduYXR1cmU=",
    };

    private static async Task<InstallationAuthenticationHandler> CreateHandlerAsync(
        ISender sender, Dictionary<string, string>? headers)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "PUT";
        httpContext.Request.Path = "/v1/platform/installations/00000000-0000-0000-0000-000000000000/backups";
        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                httpContext.Request.Headers[key] = value;
            }
        }

        var handler = new InstallationAuthenticationHandler(
            new StaticOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            sender);

        await handler.InitializeAsync(Scheme, httpContext);
        return handler;
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _value;

        public StaticOptionsMonitor(T value) => _value = value;

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
