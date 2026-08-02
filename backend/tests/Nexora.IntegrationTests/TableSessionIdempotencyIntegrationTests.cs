using System.Text;
using System.Text.Json;
using Nexora.Api.Edge.Infrastructure.Idempotency;
using Nexora.Application.Tables.Commands.OpenTableSession;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Idempotency;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-022 §12/§4 ("Duplo toque do garçom"): "duas requisições com a mesma Idempotency-Key
/// retornam a mesma sessão, sem duplicar" — provado aqui contra o
/// <see cref="IdempotencyMiddleware"/> REAL de <c>Nexora.Api.Edge</c> (a mesma classe registrada em
/// <c>Program.cs</c>, não um duplo) e o <see cref="IdempotencyStore"/> REAL sobre Postgres
/// (Testcontainers), envolvendo o pipeline MediatR real de <see cref="OpenTableSessionCommand"/>.
/// Mesma decisão de desenho documentada em <c>IdempotencyStoreTests</c>: middleware real + store
/// real + handler real prova o mesmo que um teste HTTP de ponta a ponta provaria, sem a
/// infraestrutura adicional de host/JWT que um <c>WebApplicationFactory</c> completo exigiria.
/// </summary>
[Collection("Postgres")]
public sealed class TableSessionIdempotencyIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public TableSessionIdempotencyIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Duplo_Toque_Do_Garcom_Com_A_Mesma_Idempotency_Key_Nao_Duplica_A_Sessao()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "30", "token-mesa-30");

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        var idempotencyStore = new IdempotencyStore(db);

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var executions = 0;

        var middleware = new IdempotencyMiddleware(async ctx =>
        {
            executions++;
            var result = await sender.Send(new OpenTableSessionCommand(tableId, GuestCount: 4, OccurredAt: null));
            ctx.Response.StatusCode = result.IsSuccess ? StatusCodes.Status201Created : StatusCodes.Status409Conflict;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(result.IsSuccess ? (object)result.Value! : new { code = result.Code }));
        });

        var body = """{"guestCount":4}""";

        var first = CreateContext(tableId, idempotencyKey, body);
        await middleware.InvokeAsync(first, idempotencyStore, tenantContext);

        var second = CreateContext(tableId, idempotencyKey, body);
        await middleware.InvokeAsync(second, idempotencyStore, tenantContext);

        executions.Should().Be(1, "a segunda requisição (duplo toque) nunca deve chegar ao handler de negócio");
        first.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        second.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        // Comparação estrutural (campo a campo), não byte a byte: a resposta reenviada vem da
        // coluna jsonb do Postgres, que reordena as chaves do objeto ao devolver o texto — mesma
        // ressalva documentada em
        // IdempotencyStoreTests.CompleteAsync_Grava_A_Resposta_E_Reserva_Subsequente_Continua_Bloqueada,
        // aqui levada um passo adiante (BeEquivalentTo em vez de comparar strings) porque o corpo
        // desta resposta tem várias chaves, não só uma.
        var firstSession = JsonSerializer.Deserialize<TableSessionResponse>(await ReadBodyAsync(first));
        var secondSession = JsonSerializer.Deserialize<TableSessionResponse>(await ReadBodyAsync(second));
        secondSession.Should().BeEquivalentTo(firstSession, "a segunda chamada deve devolver a MESMA sessão, sem reexecutar");
        second.Response.Headers["Idempotent-Replay"].ToString().Should().Be("true");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var sessionCount = await verifyDb.TableSessions.CountAsync(s => s.TableId == tableId);
        sessionCount.Should().Be(1, "o duplo toque nunca deveria ter criado uma segunda sessão");
    }

    private static DefaultHttpContext CreateContext(Guid tableId, string idempotencyKey, string body)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        context.Request.Method = "POST";
        context.Request.Path = $"/v1/tables/{tableId}/sessions";
        context.Request.Headers["Idempotency-Key"] = idempotencyKey;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private async Task<Guid> SeedTableAsync(Guid tenantId, Guid storeId, Guid areaId, string label, string qrToken)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var table = DiningTable.Create(tenantId, storeId, areaId, label, qrToken, seats: 4);
        db.DiningTables.Add(table);
        await db.SaveChangesAsync();
        return table.Id;
    }

    private async Task<Guid> SeedAreaAsync(Guid tenantId, Guid storeId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var area = Area.Create(tenantId, storeId, "Salão de teste");
        db.Areas.Add(area);
        await db.SaveChangesAsync();
        return area.Id;
    }

    private async Task<(Guid TenantId, Guid StoreId)> SeedTenantAndStoreAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();

        await using var storeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        storeDb.Stores.Add(Domain.Platform.Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));
        await storeDb.SaveChangesAsync();

        return (tenantId, storeId);
    }
}
