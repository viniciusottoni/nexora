using System.Diagnostics;
using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Queries.GetTableMap;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-023 §12, "Desempenho: mapa com 60 mesas renderiza em menos de 1 s em celular de entrada".
/// </summary>
/// <remarks>
/// LIMITAÇÃO DOCUMENTADA: não há como medir renderização de tela em celular real dentro deste
/// pipeline de testes de backend — o teste aqui mede o que É controlável e É o gargalo mais
/// provável do requisito: o tempo de execução de <see cref="GetTableMapQueryHandler"/> (a query
/// que serializa o payload inteiro do mapa) contra um PostgreSQL real com 60 mesas, sessões e
/// itens sintéticos. Renderização de componente React é coberta separadamente por teste de
/// componente/vitest (<c>table-map-page.test.tsx</c>), que também não mede celular real, só que
/// o React não re-renderiza células que não mudaram (memoização) — a soma dos dois é a melhor
/// aproximação disponível sem um dispositivo físico. Orçamento do teste: 1 s de "budget" de UI
/// menos uma margem generosa para round-trip de rede — pedimos que a QUERY sozinha fique bem
/// abaixo disso (400 ms) para sobrar folga de verdade para serialização HTTP + render.
/// </remarks>
[Collection("Postgres")]
public sealed class TableMapPerformanceIntegrationTests
{
    private const int TableCount = 60;
    private static readonly TimeSpan Budget = TimeSpan.FromMilliseconds(400);

    private readonly PostgresFixture _fixture;

    public TableMapPerformanceIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetTableMapQuery_Com_60_Mesas_Executa_Dentro_Do_Orcamento()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await seedDb.SaveChangesAsync();
        }

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            seedDb.Stores.Add(Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));

            var category = Category.Create(tenantId, "Pizzas");
            seedDb.Categories.Add(category);
            await seedDb.SaveChangesAsync();

            var product = Product.Create(tenantId, category.Id, "Pizza de teste");
            seedDb.Products.Add(product);
            await seedDb.SaveChangesAsync();

            var variant = ProductVariant.Create(tenantId, product.Id, "Única");
            seedDb.ProductVariants.Add(variant);
            await seedDb.SaveChangesAsync();

            var area = Area.Create(tenantId, storeId, "Salão");
            seedDb.Areas.Add(area);
            await seedDb.SaveChangesAsync();

            for (var i = 0; i < TableCount; i++)
            {
                var table = DiningTable.Create(tenantId, storeId, area.Id, (i + 1).ToString(), $"qr-perf-{i}", sortOrder: (short)i);

                // Duas em cada três mesas ficam ocupadas, com pedido e item — o cenário mais caro
                // de calcular (soma de itens + garçom + comparação de média), não o mais barato.
                if (i % 3 != 0)
                {
                    table.Occupy();
                    seedDb.DiningTables.Add(table);

                    var waiter = AppUser.Create(tenantId, $"Garçom {i}", email: null, passwordHash: null, pinHash: "hash-irrelevante");
                    seedDb.Users.Add(waiter);

                    var session = TableSession.Create(tenantId, storeId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2, waiterId: waiter.Id);
                    seedDb.TableSessions.Add(session);
                    await seedDb.SaveChangesAsync();

                    var order = Order.Create(tenantId, storeId, Channel.DineIn, $"P{i:0000}", DateOnly.FromDateTime(DateTime.UtcNow), sessionId: session.Id);
                    var item = OrderItem.Create(tenantId, order.Id, variant.Id, unitPrice: 30m);
                    order.AddItem(item);
                    seedDb.Orders.Add(order);
                    await seedDb.SaveChangesAsync();
                }
                else
                {
                    seedDb.DiningTables.Add(table);
                    await seedDb.SaveChangesAsync();
                }
            }
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        await using var provider = BuildMediatRContainer(db, new StaticTenantContext(tenantId, storeId));
        var sender = provider.GetRequiredService<ISender>();

        // "Aquece" o pool de conexão/plano de query uma vez antes de medir — o requisito é sobre
        // a operação normal em produção (conexão já aberta), não sobre o custo de handshake TCP
        // do primeiro request do processo.
        (await sender.Send(new GetTableMapQuery(MineOnly: false, TableMapSortBy.Urgency))).IsSuccess.Should().BeTrue();

        var stopwatch = Stopwatch.StartNew();
        var result = await sender.Send(new GetTableMapQuery(MineOnly: false, TableMapSortBy.Urgency));
        stopwatch.Stop();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tables.Should().HaveCount(TableCount);
        stopwatch.Elapsed.Should().BeLessThan(Budget,
            "60 mesas precisam servir bem abaixo do orçamento de 1s de UI (US-023 §12) — a query sozinha tem que sobrar folga para serialização HTTP e render");
    }

    private static ServiceProvider BuildMediatRContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services.BuildServiceProvider();
    }
}
