extern alias ApiCloud;
using ApiCloud::Nexora.Api.Cloud.Infrastructure.Platform;
using Nexora.Application.Abstractions.Platform;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Nexora.ApiTests.Platform;

/// <summary>
/// Testa a LÓGICA de <c>TenantDomainRedirectMiddleware</c> (US-143 §3.1) com um duplo de
/// <see cref="ITenantDomainRedirectResolver"/> em memória — mesmo padrão de
/// <c>IdempotencyMiddlewareTests</c>/<c>FakeIdempotencyStore</c>: instancia o middleware
/// diretamente, sem host HTTP completo.
/// </summary>
public sealed class TenantDomainRedirectMiddlewareTests
{
    private sealed class FakeResolver : ITenantDomainRedirectResolver
    {
        private readonly string? _target;
        public int CallCount { get; private set; }
        public string? LastHost { get; private set; }

        public FakeResolver(string? target) => _target = target;

        public Task<string?> ResolvePrimaryDomainAsync(string normalizedHost, CancellationToken cancellationToken)
        {
            CallCount++;
            LastHost = normalizedHost;
            return Task.FromResult(_target);
        }
    }

    [Fact]
    public async Task Sem_Alvo_De_Redirecionamento_Segue_Para_O_Proximo_Middleware()
    {
        var nextCalled = false;
        var middleware = new TenantDomainRedirectMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "donabetinha.plataforma.com.br", "/v1/public/branding");
        var resolver = new FakeResolver(target: null);

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeTrue();
        resolver.CallCount.Should().Be(1);
        resolver.LastHost.Should().Be("donabetinha.plataforma.com.br");
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Com_Dominio_Proprio_Ativo_Redireciona_302_Preservando_Caminho_E_Querystring_E_Nao_Chama_O_Proximo()
    {
        var nextCalled = false;
        var middleware = new TenantDomainRedirectMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "donabetinha.plataforma.com.br", "/v1/public/menu", "channel=DELIVERY");
        var resolver = new FakeResolver(target: "cardapio.donabetinha.com.br");

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().Should()
            .Be("https://cardapio.donabetinha.com.br/v1/public/menu?channel=DELIVERY");
    }

    [Fact]
    public async Task Metodo_Diferente_De_GET_Nunca_E_Redirecionado()
    {
        var nextCalled = false;
        var middleware = new TenantDomainRedirectMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("POST", "donabetinha.plataforma.com.br", "/v1/platform/tenants/00000000-0000-0000-0000-000000000000/domains");
        var resolver = new FakeResolver(target: "cardapio.donabetinha.com.br");

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeTrue();
        resolver.CallCount.Should().Be(0);
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    private static DefaultHttpContext CreateContext(string method, string host, string path, string? query = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Scheme = "https";
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
        if (query is not null)
        {
            context.Request.QueryString = new QueryString("?" + query);
        }

        context.Response.Body = new MemoryStream();
        return context;
    }
}
