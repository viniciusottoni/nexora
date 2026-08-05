using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Cashier.Commands.RegisterPayments;
using Nexora.Application.Cashier.Commands.ReprintReceipt;
using Nexora.Application.Cashier.Queries.GetReceipt;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Devices;
using Nexora.Infrastructure.Persistence;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>Cenários Gherkin de US-057 (Comprovante não fiscal de consumo) contra um PostgreSQL real (Testcontainers).</summary>
[Collection("Postgres")]
public sealed class ReceiptIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public ReceiptIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>US-057 §4, cenário "Geração após o pagamento".</summary>
    [Fact]
    public async Task Comprovante_E_Gerado_Apos_O_Pagamento_E_Nunca_E_Fiscal()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Grande", unitPrice: 100m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var payment = await sender.Send(new RegisterPaymentsCommand(
            sessionId, new List<PaymentInput> { new("CASH", 100m, 100m, null, null, null, 1, false) }));
        payment.IsSuccess.Should().BeTrue(payment.IsFailure ? payment.Code : string.Empty);

        var receipt = await sender.Send(new GetReceiptQuery(sessionId));

        receipt.IsSuccess.Should().BeTrue();
        receipt.Value!.Receipt.IsFiscal.Should().BeFalse();
        receipt.Value.Receipt.Total.Should().Be(100m);
        receipt.Value.Receipt.Payments.Should().ContainSingle(p => p.Method == "CASH" && p.Amount == 100m);
        receipt.Value.Receipt.Items.Should().ContainSingle(i => i.Name.Contains("Pizza Grande"));
    }

    /// <summary>US-057 §4: comprovante não existe antes do pagamento.</summary>
    [Fact]
    public async Task Comprovante_Nao_Disponivel_Antes_Do_Pagamento()
    {
        var world = await SeedWorldAsync();
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var receipt = await sender.Send(new GetReceiptQuery(sessionId));

        receipt.IsSuccess.Should().BeFalse();
        receipt.Code.Should().Be(ApiErrorCodes.TableSessionNotOpen);
    }

    /// <summary>US-057 §4, cenário "Reimpressão auditada".</summary>
    [Fact]
    public async Task Reimpressao_E_Registrada_Em_Auditoria_Com_Autor_E_Horario()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Reimpressão", unitPrice: 50m);
        var sessionId = await OpenSessionAsync(world);

        var caixa = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: caixa);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));
        var seedPayment = await sender.Send(new RegisterPaymentsCommand(
            sessionId, new List<PaymentInput> { new("CASH", 50m, 50m, null, null, null, 1, false) }));
        seedPayment.IsSuccess.Should().BeTrue(seedPayment.IsFailure ? seedPayment.Code : string.Empty);

        var reprint = await sender.Send(new ReprintReceiptCommand(sessionId, "printer-1"));

        reprint.IsSuccess.Should().BeTrue();
        reprint.Value!.Queued.Should().BeTrue();

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.Action == "RECEIPT_REPRINTED" && a.EntityId == sessionId);
        audit.ActorId.Should().Be(caixa);
        audit.OccurredAt.Should().NotBe(default);
    }

    private sealed record World(Guid TenantId, Guid StoreId, Guid AreaId);

    private async Task<World> SeedWorldAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        await using var storeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        storeDb.Stores.Add(Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));
        storeDb.Areas.Add(Area.Create(tenantId, storeId, "Salão de teste"));
        await storeDb.SaveChangesAsync();

        return new World(tenantId, storeId, (await storeDb.Areas.SingleAsync()).Id);
    }

    private async Task SeedTenantServiceFeeAsync(Guid tenantId, decimal percent)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var config = await db.TenantConfigs.SingleOrDefaultAsync(c => c.TenantId == tenantId);
        if (config is null)
        {
            config = TenantConfig.Create(tenantId);
            db.TenantConfigs.Add(config);
        }

        config.UpdateOperation($"{{\"serviceFeePercent\": {percent.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedProductAsync(Guid tenantId, string productName, decimal unitPrice)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var category = Category.Create(tenantId, "Categoria de teste");
        db.Categories.Add(category);
        var product = Product.Create(tenantId, category.Id, productName);
        db.Products.Add(product);
        var variant = ProductVariant.Create(tenantId, product.Id, "Único");
        db.ProductVariants.Add(variant);
        var price = Price.Create(tenantId, variant.Id, Channel.DineIn, unitPrice);
        db.Prices.Add(price);

        await db.SaveChangesAsync();
        return variant.Id;
    }

    private async Task<Guid> OpenSessionAsync(World world, string tableLabel = "1")
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var uniqueLabel = $"{tableLabel}-{Guid.NewGuid():N}"[..12];
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, uniqueLabel, $"qr-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();
        return session.Id;
    }

    private static ServiceProvider BuildContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton<IAlertsBroadcaster>(new RecordingAlertsBroadcaster());
        services.AddSingleton<ITableMapBroadcaster>(new RecordingTableMapBroadcaster());
        services.AddSingleton<IOrderConsumptionBroadcaster>(new RecordingOrderConsumptionBroadcaster());
        services.AddSingleton<IStationBroadcaster>(new RecordingStationBroadcaster());
        services.AddSingleton<IAuthorizationTokenValidator>(new StubAuthorizationTokenValidator());
        if (db is AppDbContext appDbContext)
        {
            services.AddSingleton<IOrderShortCodeAllocator>(new OrderShortCodeAllocator(appDbContext));
        }

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ICommand).Assembly);

        return services.BuildServiceProvider();
    }
}
