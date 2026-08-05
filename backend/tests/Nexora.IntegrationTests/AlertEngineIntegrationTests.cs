using Nexora.Application.Alerts.Commands.UpdateAlertRouting;
using Nexora.Application.Alerts.Commands.EscalatePendingAlerts;
using Nexora.Application.Alerts.Commands.EvaluateEdgeAlertConditions;
using Nexora.Application.Alerts.Queries.GetAlerts;
using Nexora.Application.Alerts.Support;
using Nexora.Application.Catalog.Availability.Commands.MarkProductAvailable;
using Nexora.Application.Catalog.Availability.Commands.MarkProductUnavailable;
using Nexora.Contracts.Alerts;
using Nexora.Domain.Catalog;
using Nexora.Domain.Metrics;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin de E-08 (US-080 motor de alertas, US-082 direcionamento/escalonamento, US-083
/// agrupamento) contra um PostgreSQL real (Testcontainers) — mesmo pipeline MediatR de produção
/// (<see cref="MediatRTestContainerFactory"/>).
/// </summary>
[Collection("Postgres")]
public sealed class AlertEngineIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public AlertEngineIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>US-080 §4, cenário "Limiar configurável por tenant": a mesma situação em dois estabelecimentos dispara conforme o próprio limiar de cada um.</summary>
    [Fact]
    public async Task Dois_Tenants_Com_Limiares_Diferentes_Disparam_Pedido_Atrasado_Conforme_O_Proprio_Limiar()
    {
        var tenantStrict = await SeedTenantAsync(orderCriticalMinutes: 5);
        // Limiar de aviso também precisa ficar acima dos 20 min do pedido semeado abaixo — senão o
        // tenant "tolerante" ainda dispararia um alerta de severidade Warning (US-080: aviso e
        // crítico são limiares independentes, não só o crítico importa).
        var tenantLenient = await SeedTenantAsync(orderWarnMinutes: 45, orderCriticalMinutes: 60);

        await SeedLateOrderAsync(tenantStrict, minutesAgo: 20);
        await SeedLateOrderAsync(tenantLenient, minutesAgo: 20);

        await RunEvaluationAsync(tenantStrict);
        await RunEvaluationAsync(tenantLenient);

        (await OpenAlertsAsync(tenantStrict, AlertTypes.OrderLate)).Should().ContainSingle();
        (await OpenAlertsAsync(tenantLenient, AlertTypes.OrderLate)).Should().BeEmpty(
            "20 minutos de atraso fica abaixo dos limiares (aviso e crítico) deste tenant mais tolerante");
    }

    /// <summary>US-080 §4, cenário "Deduplicação": condição continuar verdadeira em avaliações seguintes não cria um segundo alerta.</summary>
    [Fact]
    public async Task Avaliacao_Repetida_Nao_Duplica_O_Alerta_De_Pedido_Atrasado()
    {
        var tenant = await SeedTenantAsync(orderCriticalMinutes: 5);
        await SeedLateOrderAsync(tenant, minutesAgo: 20);

        await RunEvaluationAsync(tenant);
        await RunEvaluationAsync(tenant);
        await RunEvaluationAsync(tenant);

        (await OpenAlertsAsync(tenant, AlertTypes.OrderLate)).Should().ContainSingle();
    }

    /// <summary>US-080 §4, cenário "Resolução automática": pedido entregue encerra o alerta sem intervenção manual.</summary>
    [Fact]
    public async Task Pedido_Entregue_Resolve_O_Alerta_Automaticamente()
    {
        var tenant = await SeedTenantAsync(orderCriticalMinutes: 5);
        var orderId = await SeedLateOrderAsync(tenant, minutesAgo: 20);

        await RunEvaluationAsync(tenant);
        (await OpenAlertsAsync(tenant, AlertTypes.OrderLate)).Should().ContainSingle();

        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenant.TenantId, tenant.StoreId)))
        {
            // Canal DineIn não passa por Dispatch/MarkDelivered (exclusivo de Delivery) — o
            // caminho terminal de salão é StartProduction -> MarkReady -> Close.
            var order = await db.Orders.SingleAsync(o => o.Id == orderId);
            order.StartProduction();
            order.MarkReady();
            order.Close();
            await db.SaveChangesAsync();
        }

        await RunEvaluationAsync(tenant);

        (await OpenAlertsAsync(tenant, AlertTypes.OrderLate)).Should().BeEmpty();

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenant.TenantId, tenant.StoreId));
        var resolved = await verifyDb.Alerts.SingleAsync(a => a.EntityId == orderId && a.Type == AlertTypes.OrderLate);
        resolved.ResolvedAt.Should().NotBeNull();
    }

    /// <summary>US-080 §2 "produto indisponível" — hook reativo em MarkProductUnavailable/MarkProductAvailable (US-080 §4 "Resolução automática").</summary>
    [Fact]
    public async Task Produto_Marcado_Indisponivel_Cria_Alerta_E_Disponivel_Resolve()
    {
        var tenant = await SeedTenantAsync(orderCriticalMinutes: 18);
        var (productId, _) = await SeedProductAsync(tenant);
        var actorId = Guid.NewGuid();

        var tenantContext = new StaticTenantContext(tenant.TenantId, tenant.StoreId, actorId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var unavailable = await sender.Send(new MarkProductUnavailableCommand(productId, "OUT_OF_STOCK", AutoRestoreNextDay: false));
        unavailable.IsSuccess.Should().BeTrue();

        // US-044 §5/§4 (cenário "Alerta dirigido"): garçom, caixa e gestor — matriz padrão de
        // AlertRoutingConfig.Defaults[ProductUnavailable] (US-082 §7), sem personalização de tenant.
        var alert = (await OpenAlertsAsync(tenant, AlertTypes.ProductUnavailable)).Should().ContainSingle(a => a.EntityId == productId).Subject;
        alert.TargetRoles.Should().BeEquivalentTo(new[] { "WAITER", "CASHIER", "MANAGER" });

        var available = await sender.Send(new MarkProductAvailableCommand(productId));
        available.IsSuccess.Should().BeTrue();

        (await OpenAlertsAsync(tenant, AlertTypes.ProductUnavailable)).Should().BeEmpty();
    }

    /// <summary>US-082 §4, cenário "Escalonamento por falta de resposta": alerta sem reconhecimento além do prazo escala para o gestor.</summary>
    [Fact]
    public async Task Alerta_Sem_Reconhecimento_Escala_Para_O_Gestor_Apos_O_Prazo()
    {
        var tenant = await SeedTenantAsync(orderCriticalMinutes: 5);
        var tenantContext = new StaticTenantContext(tenant.TenantId, tenant.StoreId);

        await using (var routingDb = _fixture.CreateAppDbContext(tenantContext))
        await using (var routingProvider = MediatRTestContainerFactory.Build(routingDb, tenantContext))
        {
            var routingSender = routingProvider.GetRequiredService<ISender>();
            var routingPatch = new Dictionary<string, AlertRoutingRulePatch>
            {
                [AlertTypes.OrderLate] = new(
                    Roles: new[] { "WAITER", "KITCHEN" },
                    Scope: AlertRoutingScopes.Responsible,
                    EscalateAfterSeconds: 120,
                    GroupWindowSeconds: 60),
            };

            var routingResult = await routingSender.Send(new UpdateAlertRoutingCommand(routingPatch));
            routingResult.IsSuccess.Should().BeTrue();
        }

        await SeedLateOrderAsync(tenant, minutesAgo: 20);
        await RunEvaluationAsync(tenant);

        // ORDER_LATE tem escalateAfterSeconds=120 por padrão (AlertRoutingConfig.Defaults) — em vez
        // de esperar 2 minutos reais, adianta RaisedAt via SQL direto (o alerta em si já foi criado
        // pelo caminho real do motor acima; só o RELÓGIO do teste é acelerado).
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenant.TenantId, tenant.StoreId)))
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE alert SET raised_at = {DateTimeOffset.UtcNow.AddMinutes(-10)} WHERE type = {AlertTypes.OrderLate}");
        }

        await using var escalationDb = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(escalationDb, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new EscalatePendingAlertsCommand(tenant.TenantId));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        var escalated = (await OpenAlertsAsync(tenant, AlertTypes.OrderLate)).Single();
        escalated.TargetRoles.Should().Contain("MANAGER");
    }

    /// <summary>US-083 §4, cenário "Rajada agrupada": vários pedidos atrasados na mesma janela compartilham grupo e geram uma notificação consolidada.</summary>
    [Fact]
    public async Task Rajada_De_Pedidos_Atrasados_Compartilha_Grupo_E_Consolida_A_Notificacao()
    {
        var tenant = await SeedTenantAsync(orderCriticalMinutes: 5);
        await SeedLateOrderAsync(tenant, minutesAgo: 20);
        await SeedLateOrderAsync(tenant, minutesAgo: 21);
        await SeedLateOrderAsync(tenant, minutesAgo: 22);

        await RunEvaluationAsync(tenant);

        var tenantContext = new StaticTenantContext(tenant.TenantId, tenant.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var grouped = await sender.Send(new GetGroupedAlertsQuery());
        grouped.IsSuccess.Should().BeTrue();

        var group = grouped.Value!.Groups.Single(g => g.Type == AlertTypes.OrderLate);
        group.Count.Should().Be(3);
        group.Message.Should().Be("3 pedidos atrasados");
        group.Alerts.Should().HaveCount(3);
    }

    /// <summary>US-081 §7 <c>push_subscription</c> — tabela nova (E-08): prova a política tenant_isolation independentemente da suíte genérica de US-001.</summary>
    [Fact]
    public async Task PushSubscription_E_Isolada_Por_Tenant_Pela_Politica_RLS()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantA, $"tenant-{tenantA:N}", "Tenant A"));
            seedDb.Tenants.Add(Tenant.Create(tenantB, $"tenant-{tenantB:N}", "Tenant B"));
            await seedDb.SaveChangesAsync();
        }

        await using (var dbA = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA)))
        {
            dbA.PushSubscriptions.Add(PushSubscription.Create(
                tenantA, Guid.NewGuid(), "https://push.example/a", "p256dh-a", "auth-a"));
            await dbA.SaveChangesAsync();
        }

        await using var dbB = _fixture.CreateAppDbContext(new StaticTenantContext(tenantB));
        var visibleFromB = await dbB.PushSubscriptions.ToListAsync();

        visibleFromB.Should().BeEmpty("RLS deve impedir que o tenant B veja assinaturas de push do tenant A");
    }

    private sealed record TenantWorld(Guid TenantId, Guid StoreId);

    private async Task<TenantWorld> SeedTenantAsync(int orderCriticalMinutes, int orderWarnMinutes = 1)
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste E-08"));
            await db.SaveChangesAsync();
        }

        await using var storeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        storeDb.Stores.Add(Domain.Platform.Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));

        var config = TenantConfig.Create(tenantId);
        config.UpdateThresholds(
            $$"""{"orderWarnMinutes": {{orderWarnMinutes}}, "orderCriticalMinutes": {{orderCriticalMinutes}}}""");
        storeDb.TenantConfigs.Add(config);

        await storeDb.SaveChangesAsync();

        return new TenantWorld(tenantId, storeId);
    }

    private async Task<Guid> SeedLateOrderAsync(TenantWorld tenant, int minutesAgo)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenant.TenantId, tenant.StoreId));

        var order = Order.Create(
            tenant.TenantId, tenant.StoreId, Channel.DineIn, ShortCode(),
            DateOnly.FromDateTime(DateTime.UtcNow));
        order.Place(DateTimeOffset.UtcNow.AddMinutes(-minutesAgo));
        db.Orders.Add(order);

        await db.SaveChangesAsync();

        return order.Id;
    }

    private async Task<(Guid ProductId, Guid VariantId)> SeedProductAsync(TenantWorld tenant)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenant.TenantId, tenant.StoreId));

        var category = Category.Create(tenant.TenantId, "Categoria de teste");
        db.Categories.Add(category);

        var product = Product.Create(tenant.TenantId, category.Id, "Produto de teste");
        db.Products.Add(product);

        var variant = ProductVariant.Create(tenant.TenantId, product.Id, "Único");
        db.ProductVariants.Add(variant);

        await db.SaveChangesAsync();

        return (product.Id, variant.Id);
    }

    private async Task RunEvaluationAsync(TenantWorld tenant)
    {
        var tenantContext = new StaticTenantContext(tenant.TenantId, tenant.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new EvaluateEdgeAlertConditionsCommand());
        result.IsSuccess.Should().BeTrue();
    }

    private async Task<List<Alert>> OpenAlertsAsync(TenantWorld tenant, string type)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenant.TenantId, tenant.StoreId));
        return await db.Alerts.Where(a => a.Type == type && a.ResolvedAt == null).ToListAsync();
    }

    private static string ShortCode() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
