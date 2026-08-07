using System.Text;
using Nexora.Api.Edge.Infrastructure.Idempotency;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.ApiTests.Idempotency;

/// <summary>
/// Testa a LÓGICA de <c>Nexora.Api.Edge.Infrastructure.Idempotency.IdempotencyMiddleware</c>
/// (ADR-020) com um duplo de <see cref="Nexora.Application.Abstractions.Idempotency.IIdempotencyStore"/>
/// em memória (<see cref="FakeIdempotencyStore"/>) — sem precisar de banco nem de host HTTP
/// completo. O gêmeo em <c>Nexora.Api.Cloud</c> é código idêntico linha a linha (mesma classe,
/// só o namespace muda) — cobrir um cobre o comportamento do outro; o mecanismo de armazenamento
/// que ambos compartilham (<c>Nexora.Infrastructure.Idempotency.IdempotencyStore</c>) é provado
/// contra Postgres real em <c>Nexora.IntegrationTests.IdempotencyStoreTests</c>.
/// </summary>
public sealed class IdempotencyMiddlewareTests
{
    private sealed class FakeTenantContext : ICurrentTenantContext
    {
        public Guid? TenantId => Guid.NewGuid();
        public Guid? StoreId => null;
        public Guid? UserId => null;
        public IReadOnlyList<string> Roles { get; } = Array.Empty<string>();
        public IReadOnlyList<string> Permissions { get; } = Array.Empty<string>();
        public Guid? DeviceId => null;
        public Guid? SessionId => null;
    }

    [Fact]
    public async Task Sem_Header_Retorna_422_Idempotency_Key_Required_E_Nao_Executa_O_Proximo_Middleware()
    {
        var nextCalled = false;
        var middleware = new IdempotencyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("POST", "/v1/roles", idempotencyKey: null, body: "{}");

        await middleware.InvokeAsync(context, new FakeIdempotencyStore(), new FakeTenantContext());

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        (await ReadResponseAsync(context)).Should().Contain("\"code\":\"IDEMPOTENCY_KEY_REQUIRED\"");
    }

    [Fact]
    public async Task Metodo_De_Leitura_Nao_Exige_Header_Nem_Passa_Pelo_Store()
    {
        var nextCalled = false;
        var middleware = new IdempotencyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "/v1/roles", idempotencyKey: null, body: null);
        var store = new FakeIdempotencyStore();

        await middleware.InvokeAsync(context, store, new FakeTenantContext());

        nextCalled.Should().BeTrue();
        store.BeginCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Endpoint_Marcado_Isento_Pula_A_Verificacao()
    {
        var nextCalled = false;
        var middleware = new IdempotencyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("POST", "/v1/auth/pin", idempotencyKey: null, body: "{}");
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new IdempotencyExemptAttribute()),
            "pin"));

        await middleware.InvokeAsync(context, new FakeIdempotencyStore(), new FakeTenantContext());

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK); // default, nada escreveu 422/409
    }

    [Fact]
    public async Task Primeira_Chamada_Executa_E_Grava_A_Resposta_Para_Reenvio()
    {
        var middleware = new IdempotencyMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("""{"id":"device-1"}""");
        });
        var store = new FakeIdempotencyStore();
        var context = CreateContext("POST", "/v1/devices/pair", idempotencyKey: "intent-1", body: "{\"code\":\"ABC\"}");

        await middleware.InvokeAsync(context, store, new FakeTenantContext());

        context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        (await ReadResponseAsync(context)).Should().Be("""{"id":"device-1"}""");
        context.Response.Headers["Idempotent-Replay"].ToString().Should().Be("false");
        store.BeginCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Segunda_Chamada_Com_A_Mesma_Chave_E_Payload_Devolve_A_Mesma_Resposta_Sem_Reexecutar()
    {
        var executions = 0;
        var middleware = new IdempotencyMiddleware(async ctx =>
        {
            executions++;
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            await ctx.Response.WriteAsync("""{"id":"device-1"}""");
        });
        var store = new FakeIdempotencyStore();
        var tenantContext = new FakeTenantContext();

        var first = CreateContext("POST", "/v1/devices/pair", idempotencyKey: "intent-1", body: "{\"code\":\"ABC\"}");
        await middleware.InvokeAsync(first, store, tenantContext);

        var second = CreateContext("POST", "/v1/devices/pair", idempotencyKey: "intent-1", body: "{\"code\":\"ABC\"}");
        await middleware.InvokeAsync(second, store, tenantContext);

        // O ponto central do ADR-020: a lógica de negócio (o "próximo" middleware/handler) só
        // roda UMA vez — a segunda chamada nunca chega lá, então nunca duplica o efeito colateral
        // (aqui simulado pelo contador `executions`; num handler real seria "criar um Device").
        executions.Should().Be(1);
        second.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        (await ReadResponseAsync(second)).Should().Be("""{"id":"device-1"}""");
        second.Response.Headers["Idempotent-Replay"].ToString().Should().Be("true");
    }

    [Fact]
    public async Task Campo_Secreto_E_Exibido_Na_Primeira_Resposta_E_Redatado_No_Replay()
    {
        var executions = 0;
        var middleware = new IdempotencyMiddleware(async context =>
        {
            executions++;
            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"credentialId":"credential-1","installToken":"secret-value","installCommand":"install secret-value"}""");
        });
        var store = new FakeIdempotencyStore();
        var tenantContext = new FakeTenantContext();

        var first = CreateContext("POST", "/v1/platform/installations/installation-1/tokens", "intent-secret", "{}");
        SetSecretRedactionEndpoint(first);
        await middleware.InvokeAsync(first, store, tenantContext);

        var second = CreateContext("POST", "/v1/platform/installations/installation-1/tokens", "intent-secret", "{}");
        SetSecretRedactionEndpoint(second);
        await middleware.InvokeAsync(second, store, tenantContext);

        executions.Should().Be(1);
        (await ReadResponseAsync(first)).Should().Contain("secret-value");
        (await ReadResponseAsync(second)).Should().Be(
            """{"credentialId":"credential-1","installToken":null,"installCommand":null}""");
        second.Response.Headers["Idempotent-Replay"].ToString().Should().Be("true");
    }

    [Fact]
    public async Task Mesma_Chave_Com_Payload_Diferente_Retorna_422_Idempotency_Key_Reused()
    {
        var executions = 0;
        var middleware = new IdempotencyMiddleware(async ctx =>
        {
            executions++;
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            await ctx.Response.WriteAsync("{}");
        });
        var store = new FakeIdempotencyStore();
        var tenantContext = new FakeTenantContext();

        var first = CreateContext("POST", "/v1/devices/pair", idempotencyKey: "intent-1", body: "{\"code\":\"ABC\"}");
        await middleware.InvokeAsync(first, store, tenantContext);

        var second = CreateContext("POST", "/v1/devices/pair", idempotencyKey: "intent-1", body: "{\"code\":\"DIFFERENT\"}");
        await middleware.InvokeAsync(second, store, tenantContext);

        executions.Should().Be(1); // a segunda NUNCA executa a lógica de negócio
        second.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        (await ReadResponseAsync(second)).Should().Contain("\"code\":\"IDEMPOTENCY_KEY_REUSED\"");
    }

    [Fact]
    public async Task Requisicao_Concorrente_Com_A_Mesma_Chave_Retorna_409_Request_In_Progress()
    {
        var store = new FakeIdempotencyStore();
        var tenantContext = new FakeTenantContext();
        // Simula a primeira requisição ainda "em voo" — reserva a chave sem completar.
        await store.BeginAsync("intent-1", tenantContext.TenantId, "POST /v1/devices/pair", ComputeHash("{\"code\":\"ABC\"}"), DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);

        var middleware = new IdempotencyMiddleware(_ => throw new InvalidOperationException("não deveria executar"));
        var context = CreateContext("POST", "/v1/devices/pair", idempotencyKey: "intent-1", body: "{\"code\":\"ABC\"}");

        await middleware.InvokeAsync(context, store, tenantContext);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        (await ReadResponseAsync(context)).Should().Contain("\"code\":\"REQUEST_IN_PROGRESS\"");
    }

    [Fact]
    public async Task Falha_5xx_Descarta_A_Reserva_E_Permite_Reenvio_Real()
    {
        var attempt = 0;
        var middleware = new IdempotencyMiddleware(async ctx =>
        {
            attempt++;
            if (attempt == 1)
            {
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await ctx.Response.WriteAsync("{}");
                return;
            }

            ctx.Response.StatusCode = StatusCodes.Status201Created;
            await ctx.Response.WriteAsync("""{"id":"device-1"}""");
        });
        var store = new FakeIdempotencyStore();
        var tenantContext = new FakeTenantContext();

        var first = CreateContext("POST", "/v1/devices/pair", idempotencyKey: "intent-1", body: "{\"code\":\"ABC\"}");
        await middleware.InvokeAsync(first, store, tenantContext);
        first.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        // ADR-020: "requisição original falhou com 5xx -> não armazena" — o reenvio com a MESMA
        // chave deve executar de novo (não 409, não replay).
        var second = CreateContext("POST", "/v1/devices/pair", idempotencyKey: "intent-1", body: "{\"code\":\"ABC\"}");
        await middleware.InvokeAsync(second, store, tenantContext);

        attempt.Should().Be(2);
        second.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    private static string ComputeHash(string body)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    private static void SetSecretRedactionEndpoint(HttpContext context)
    {
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new IdempotencyRedactFieldsAttribute("installToken", "installCommand")),
            "installation-token-reissue"));
    }

    private static DefaultHttpContext CreateContext(string method, string path, string? idempotencyKey, string? body)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        context.Request.Method = method;
        context.Request.Path = path;
        if (idempotencyKey is not null)
        {
            context.Request.Headers["Idempotency-Key"] = idempotencyKey;
        }

        if (body is not null)
        {
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentLength = context.Request.Body.Length;
        }

        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
