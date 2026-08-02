using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Tables;
using Nexora.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Nexora.ApiTests.Realtime;

/// <summary>
/// US-023 §12 ("Atualização em tempo real em menos de 2 s por WebSocket") e ADR-011 (salas por
/// tenant, derivadas das claims do token). Sobe o host real do <c>Nexora.Api.Edge</c> em memória
/// (<see cref="WebApplicationFactory{TEntryPoint}"/>/<see cref="TestServer"/>) e conecta um
/// <see cref="HubConnection"/> de verdade contra <c>/hubs/table-map</c>, usando
/// <see cref="TestServer.CreateWebSocketClient"/> como transporte — WebSocket real dentro do
/// processo de teste (não um mock de Hub), a única diferença de produção é o socket não sair para
/// a rede. Não usa Postgres: nada neste teste consulta o banco (o hub só lê a claim "tid" do
/// token e o broadcaster é chamado diretamente via DI para simular "um pedido foi confirmado"),
/// então a connection string configurada é só um valor sintaticamente válido, nunca aberto.
/// </summary>
public sealed class TableMapHubTests : IDisposable
{
    private const string TestJwtSecret = "api-tests-jwt-secret-com-pelo-menos-32-bytes-nao-e-producao";
    private const string TestIssuer = "nexora-tests";
    private const string TestAudience = "nexora-tests-apps";

    private readonly WebApplicationFactory<Program> _factory;

    public TableMapHubTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Environment != "Development": ASP.NET Core só liga ServiceProviderOptions.Validate*
            // automaticamente em Development. Sem isto, o Build() falha eagerly porque o único
            // assembly de Application (CLAUDE.md — não há módulos separados por assembly) registra
            // no MediatR TODOS os handlers, inclusive os exclusivos do Api.Cloud (ex.: envio de
            // e-mail, storage de branding) que o Api.Edge nunca invoca e por isso nunca precisou
            // resolver — o mesmo aconteceria em qualquer `dotnet run` local do Api.Edge em
            // Development; produção roda em Production/Docker, então isto reflete o ambiente real.
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=127.0.0.1;Database=unused;Username=unused;Password=unused");
            builder.UseSetting("Auth:Jwt:Secret", TestJwtSecret);
            builder.UseSetting("Auth:Jwt:Issuer", TestIssuer);
            builder.UseSetting("Auth:Jwt:Audience", TestAudience);
            builder.UseSetting("Auth:Secrets:PinLookupPepper", "api-tests-pin-lookup-pepper-32-bytes-minimo!!");
            builder.UseSetting("Devices:Security:Pepper", "api-tests-device-pepper-32-bytes-no-minimo!!!!");
            builder.UseSetting("Edge:Installation:TenantId", Guid.NewGuid().ToString());
            builder.UseSetting("Edge:Installation:InstallationId", Guid.NewGuid().ToString());
        });
    }

    [Fact]
    public async Task Broadcast_Chega_Ao_Cliente_Do_Mesmo_Tenant_Em_Menos_De_Dois_Segundos()
    {
        var tenantId = Guid.NewGuid();
        var token = await IssueTokenAsync(tenantId);

        var connection = BuildConnection(token);
        var received = new TaskCompletionSource<TableMapEntryResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<TableMapEntryResponse>("table.changed", table => received.TrySetResult(table));

        await connection.StartAsync();
        try
        {
            var payload = new TableMapEntryResponse(
                Guid.NewGuid(),
                "12",
                "Salão",
                "OCCUPIED",
                4,
                new TableMapSessionResponse(DateTimeOffset.UtcNow, 5, 42.50m, 3, null, Guid.NewGuid()),
                new TableMapFlagsResponse(false, false, 0, false));

            using (var scope = _factory.Services.CreateScope())
            {
                var broadcaster = scope.ServiceProvider.GetRequiredService<ITableMapBroadcaster>();
                // Simula "um pedido foi confirmado em outra mesa" (Gherkin "Atualização em tempo
                // real") — nenhum command desta solution chama isto ainda (ver docstring de
                // ITableMapBroadcaster); a US-023 só precisa provar que o transporte funciona.
                await broadcaster.NotifyTableChangedAsync(tenantId, payload, CancellationToken.None);
            }

            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            completed.Should().Be(received.Task, "US-023 §4 exige que a mudança chegue ao mapa aberto em até 2 segundos");

            var table = await received.Task;
            table.Id.Should().Be(payload.Id);
            table.Label.Should().Be("12");
            table.Session!.Total.Should().Be(42.50m);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Cliente_De_Outro_Tenant_Nao_Recebe_O_Broadcast()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var token = await IssueTokenAsync(otherTenantId);

        var connection = BuildConnection(token);
        var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<TableMapEntryResponse>("table.changed", _ => received.TrySetResult(true));

        await connection.StartAsync();
        try
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var broadcaster = scope.ServiceProvider.GetRequiredService<ITableMapBroadcaster>();
                await broadcaster.NotifyTableChangedAsync(
                    tenantId,
                    new TableMapEntryResponse(Guid.NewGuid(), "1", "Salão", "FREE", 4, null, new TableMapFlagsResponse(false, false, 0, false)),
                    CancellationToken.None);
            }

            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromMilliseconds(500)));
            completed.Should().NotBe(received.Task, "ADR-011: a sala é por tenant — cliente de outro tenant nunca deveria ouvir este evento");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private HubConnection BuildConnection(string token)
    {
        var server = _factory.Server;
        return new HubConnectionBuilder()
            .AddJsonProtocol(options =>
            {
                // Precisa espelhar o MoneyJsonConverter do servidor (Program.cs,
                // AddSignalR().AddJsonProtocol) — sem isto, o cliente recebe "total":"42.50" (ADR-017,
                // dinheiro como string) e falha ao desserializar decimal, derrubando a mensagem
                // silenciosamente em vez de invocar o handler "table.changed".
                options.PayloadSerializerOptions.Converters.Add(new MoneyJsonConverter());
                options.PayloadSerializerOptions.Converters.Add(new NullableMoneyJsonConverter());
            })
            .WithUrl(new Uri(server.BaseAddress, "/hubs/table-map"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    // Um WebSocketFactory customizado assume a conexão por inteiro — diferente do
                    // transporte padrão do cliente SignalR, ele NÃO anexa "access_token" à
                    // querystring sozinho (confirmado empiricamente: sem isto, a URL chega ao
                    // servidor só com "?id=...", e o handshake cai em 401). Mesmo mecanismo que um
                    // navegador real usaria (ADR-011: WS não permite header Authorization).
                    var separator = context.Uri.Query.Length > 0 ? "&" : "?";
                    var authenticatedUri = new Uri($"{context.Uri}{separator}access_token={Uri.EscapeDataString(token)}");

                    var client = server.CreateWebSocketClient();
                    return await client.ConnectAsync(authenticatedUri, cancellationToken);
                };
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
            })
            .Build();
    }

    private static async Task<string> IssueTokenAsync(Guid tenantId)
    {
        var issuer = new JwtTokenIssuer(Options.Create(new JwtOptions
        {
            Secret = TestJwtSecret,
            Issuer = TestIssuer,
            Audience = TestAudience,
        }));

        var claims = new AccessClaims(
            Subject: Guid.NewGuid(),
            TenantId: tenantId,
            StoreId: Guid.NewGuid(),
            Roles: new[] { "WAITER" },
            Permissions: new[] { "table:read" });

        return await issuer.IssueAccessTokenAsync(claims, ttlSeconds: 300);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
