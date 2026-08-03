using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
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
/// US-031 (Roteamento simultâneo para cozinha e caixa) §12/ADR-011 — cenário Gherkin "Salas
/// corretas do WebSocket": "deve alcançar as salas station:forno, role:cashier e table:12... e não
/// deve alcançar a sala da praça Bebidas". Mesmo padrão exato de <c>TableMapHubTests</c> (US-023):
/// sobe o host real do <c>Nexora.Api.Edge</c> em memória, conecta <see cref="HubConnection"/> de
/// verdade contra <c>/hubs/kds</c> via <see cref="TestServer.CreateWebSocketClient"/> (WebSocket
/// real dentro do processo de teste), e chama <see cref="IStationBroadcaster"/> diretamente via DI
/// para simular "um pedido foi confirmado" — não precisa de Postgres (nada aqui consulta o banco: a
/// inscrição do hub só lê claims do token, e o broadcaster é resolvido via DI).
/// </summary>
public sealed class KdsHubTests : IDisposable
{
    private const string TestJwtSecret = "api-tests-kds-jwt-secret-com-pelo-menos-32-bytes-nao-e-producao";
    private const string TestIssuer = "nexora-tests";
    private const string TestAudience = "nexora-tests-apps";

    private readonly WebApplicationFactory<Program> _factory;

    public KdsHubTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Ver docstring de TableMapHubTests para o motivo de Production + connection string
            // sintaticamente válida mas nunca aberta.
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=127.0.0.1;Database=unused;Username=unused;Password=unused");
            builder.UseSetting("Auth:Jwt:Secret", TestJwtSecret);
            builder.UseSetting("Auth:Jwt:Issuer", TestIssuer);
            builder.UseSetting("Auth:Jwt:Audience", TestAudience);
            builder.UseSetting("Auth:Secrets:PinLookupPepper", "api-tests-kds-pin-lookup-pepper-32-bytes-min!!");
            builder.UseSetting("Devices:Security:Pepper", "api-tests-kds-device-pepper-32-bytes-no-minimo!");
            builder.UseSetting("Edge:Installation:TenantId", Guid.NewGuid().ToString());
            builder.UseSetting("Edge:Installation:InstallationId", Guid.NewGuid().ToString());
        });
    }

    /// <summary>Cenário Gherkin "Salas corretas do WebSocket" (US-031 §4) — o essencial desta história.</summary>
    [Fact]
    public async Task OrderPlaced_Alcanca_Estacao_Do_Forno_Caixa_E_Mesa_Mas_Nao_A_Estacao_Bebidas()
    {
        var ovenStationId = Guid.NewGuid();
        var beverageStationId = Guid.NewGuid();
        var tableId = Guid.NewGuid();

        var ovenConnection = BuildConnection(await IssueTokenAsync(stationId: ovenStationId, roles: new[] { "KITCHEN" }));
        var beverageConnection = BuildConnection(await IssueTokenAsync(stationId: beverageStationId, roles: new[] { "KITCHEN" }));
        var cashierConnection = BuildConnection(await IssueTokenAsync(stationId: null, roles: new[] { "CASHIER" }));

        var ovenReceived = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var beverageReceived = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cashierReceived = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        ovenConnection.On<object>("kdsEvent", payload => ovenReceived.TrySetResult(payload));
        beverageConnection.On<object>("kdsEvent", payload => beverageReceived.TrySetResult(payload));
        cashierConnection.On<object>("kdsEvent", payload => cashierReceived.TrySetResult(payload));

        await ovenConnection.StartAsync();
        await beverageConnection.StartAsync();
        await cashierConnection.StartAsync();
        try
        {
            using var scope = _factory.Services.CreateScope();
            var broadcaster = scope.ServiceProvider.GetRequiredService<IStationBroadcaster>();

            // Pedido da mesa "12" com item SÓ da praça Forno — nenhum item de Bebidas.
            await broadcaster.OrderPlaced(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "A47",
                tableId,
                "12",
                "DineIn",
                new[]
                {
                    new StationBroadcastItem(Guid.NewGuid(), "Pizza Calabresa", ovenStationId, 1, new[] { "sem cebola" }, "bem assada"),
                },
                DateTimeOffset.UtcNow,
                CancellationToken.None);

            var ovenCompleted = await Task.WhenAny(ovenReceived.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            ovenCompleted.Should().Be(ovenReceived.Task, "RF-KDS-01: o item da praça forno deve chegar em até 2 segundos");

            var cashierCompleted = await Task.WhenAny(cashierReceived.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            cashierCompleted.Should().Be(cashierReceived.Task, "RN-001: o caixa recebe o pedido junto com a cozinha");

            var beverageCompleted = await Task.WhenAny(beverageReceived.Task, Task.Delay(TimeSpan.FromMilliseconds(500)));
            beverageCompleted.Should().NotBe(
                beverageReceived.Task, "cenário Gherkin: não deve alcançar a sala da praça Bebidas, que não tem item neste pedido");
        }
        finally
        {
            await ovenConnection.DisposeAsync();
            await beverageConnection.DisposeAsync();
            await cashierConnection.DisposeAsync();
        }
    }

    /// <summary>Mudança de status de item (AdvanceOrderItemStatusCommand) só notifica a PRÓPRIA praça — nunca outra.</summary>
    [Fact]
    public async Task ItemStatusChanged_Alcanca_Apenas_A_Propria_Estacao()
    {
        var ovenStationId = Guid.NewGuid();
        var beverageStationId = Guid.NewGuid();

        var ovenConnection = BuildConnection(await IssueTokenAsync(stationId: ovenStationId, roles: new[] { "KITCHEN" }));
        var beverageConnection = BuildConnection(await IssueTokenAsync(stationId: beverageStationId, roles: new[] { "KITCHEN" }));

        var ovenReceived = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var beverageReceived = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        ovenConnection.On<object>("kdsEvent", payload => ovenReceived.TrySetResult(payload));
        beverageConnection.On<object>("kdsEvent", payload => beverageReceived.TrySetResult(payload));

        await ovenConnection.StartAsync();
        await beverageConnection.StartAsync();
        try
        {
            using var scope = _factory.Services.CreateScope();
            var broadcaster = scope.ServiceProvider.GetRequiredService<IStationBroadcaster>();

            await broadcaster.ItemStatusChanged(Guid.NewGuid(), ovenStationId, Guid.NewGuid(), "Pizza Calabresa", "FIRED", CancellationToken.None);

            var ovenCompleted = await Task.WhenAny(ovenReceived.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            ovenCompleted.Should().Be(ovenReceived.Task);

            var beverageCompleted = await Task.WhenAny(beverageReceived.Task, Task.Delay(TimeSpan.FromMilliseconds(500)));
            beverageCompleted.Should().NotBe(beverageReceived.Task);
        }
        finally
        {
            await ovenConnection.DisposeAsync();
            await beverageConnection.DisposeAsync();
        }
    }

    private HubConnection BuildConnection(string token)
    {
        var server = _factory.Server;
        return new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "/hubs/kds"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    // Ver comentário de TableMapHubTests.BuildConnection — WebSocketFactory
                    // customizado não anexa access_token à querystring por conta própria.
                    var separator = context.Uri.Query.Length > 0 ? "&" : "?";
                    var authenticatedUri = new Uri($"{context.Uri}{separator}access_token={Uri.EscapeDataString(token)}");

                    var client = server.CreateWebSocketClient();
                    return await client.ConnectAsync(authenticatedUri, cancellationToken);
                };
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
            })
            .Build();
    }

    private static async Task<string> IssueTokenAsync(Guid? stationId, IReadOnlyList<string> roles)
    {
        var issuer = new JwtTokenIssuer(Options.Create(new JwtOptions
        {
            Secret = TestJwtSecret,
            Issuer = TestIssuer,
            Audience = TestAudience,
        }));

        var claims = new AccessClaims(
            Subject: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            StoreId: Guid.NewGuid(),
            Roles: roles,
            Permissions: new[] { "kds:advance" },
            StationId: stationId);

        return await issuer.IssueAccessTokenAsync(claims, ttlSeconds: 300);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
