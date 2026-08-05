using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Commands.AuthorizeSensitiveAction;
using Nexora.Application.Cashier.Commands.ApplyDiscount;
using Nexora.Application.Cashier.Commands.WaiveSessionServiceFee;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Domain.Catalog;
using Nexora.Domain.Metrics;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Auth;
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
using Microsoft.Extensions.Options;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin de US-054 (Desconto com autorização) e US-053 (Taxa de serviço com retirada
/// registrada) contra um PostgreSQL real (Testcontainers). Arquivo com container PRÓPRIO (mesmo
/// espírito de <c>PendingItemsOnCloseIntegrationTests</c>, US-035): US-054 exige a pilha REAL de
/// autorização pontual (ADR-023) para o cenário "acima do limite".
/// </summary>
[Collection("Postgres")]
public sealed class DiscountAndServiceFeeIntegrationTests
{
    private const string TestJwtSecret = "discount-service-fee-integration-test-jwt-secret-32b!!";
    private const string TestPinLookupPepper = "discount-service-fee-integration-test-pin-pepper-32b!!";

    private readonly PostgresFixture _fixture;

    public DiscountAndServiceFeeIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>US-054 §4, cenário "Desconto dentro do limite": 3% com limite de 5% aplica sem autorização, mas ainda registrado com autor e motivo.</summary>
    [Fact]
    public async Task Desconto_Dentro_Do_Limite_E_Aplicado_Sem_Autorizacao_E_Registrado()
    {
        var world = await SeedWorldAsync();
        await SeedDiscountLimitAsync(world.TenantId, 5m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Doce", unitPrice: 100m);
        var sessionId = await OpenSessionAsync(world);

        var caixa = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: caixa);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var result = await sender.Send(new ApplyDiscountCommand(sessionId, Percent: 3m, Amount: null, "cortesia", "SESSION", null, null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Session.DiscountPercent.Should().Be(3m);
        result.Value.AuthorizedBy.Should().BeNull();

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.Action == "DISCOUNT_APPLIED" && a.EntityId == sessionId);
        audit.ActorId.Should().Be(caixa);
        audit.Reason.Should().Be("cortesia");
        audit.AuthorizedBy.Should().BeNull();
    }

    /// <summary>US-054 §4, cenário "Desconto acima do limite": 15% com limite de 5% sem token -> 403 com meta do limite.</summary>
    [Fact]
    public async Task Desconto_Acima_Do_Limite_Sem_Token_E_Recusado()
    {
        var world = await SeedWorldAsync();
        await SeedDiscountLimitAsync(world.TenantId, 5m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Especial", unitPrice: 100m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var result = await sender.Send(new ApplyDiscountCommand(sessionId, Percent: 15m, Amount: null, "cortesia grande", "SESSION", null, null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.AuthorizationRequired);
        result.Errors!["limitPercent"].Single().Should().Be("5");
        result.Errors!["requestedPercent"].Single().Should().Be("15");
    }

    /// <summary>US-054 §4, cenário "Desconto acima do limite": com PIN de gerente autorizado, aplica e registra executor E autorizador distintos.</summary>
    [Fact]
    public async Task Desconto_Acima_Do_Limite_Com_Autorizacao_E_Aplicado_E_Registra_Executor_E_Autorizador()
    {
        var world = await SeedWorldAsync();
        await SeedDiscountLimitAsync(world.TenantId, 5m);
        const string managerPin = "9911";
        var managerId = await SeedManagerWithPermissionAsync(world, managerPin, "cash:discount_any");
        var produto = await SeedProductAsync(world.TenantId, "Pizza Premium", unitPrice: 100m);
        var sessionId = await OpenSessionAsync(world);

        var caixa = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: caixa, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var authorized = await sender.Send(new AuthorizeSensitiveActionCommand(
            "DISCOUNT_ABOVE_LIMIT", managerPin, new Dictionary<string, object?> { ["sessionId"] = sessionId.ToString() }));
        authorized.IsSuccess.Should().BeTrue(authorized.IsFailure ? authorized.Code : string.Empty);

        var result = await sender.Send(new ApplyDiscountCommand(
            sessionId, Percent: 15m, Amount: null, "cortesia diretoria", "SESSION", null, authorized.Value!.AuthorizationToken));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.AuthorizedBy.Should().NotBeNull();
        result.Value.AuthorizedBy!.Id.Should().Be(managerId);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.Action == "DISCOUNT_APPLIED" && a.EntityId == sessionId);
        audit.ActorId.Should().Be(caixa, "executor — quem estava na tela");
        audit.AuthorizedBy.Should().Be(managerId, "autorizador — o gerente que informou o PIN");
    }

    /// <summary>US-054 §4, cenário "Desconto em valor absoluto": R$20 de desconto numa base de R$100 vira 20%.</summary>
    [Fact]
    public async Task Desconto_Em_Valor_Absoluto_Calcula_O_Percentual_Equivalente()
    {
        var world = await SeedWorldAsync();
        await SeedDiscountLimitAsync(world.TenantId, 50m); // isola do fluxo de autorização — só o cálculo percentual↔valor é testado aqui
        var produto = await SeedProductAsync(world.TenantId, "Pizza Simples", unitPrice: 100m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var result = await sender.Send(new ApplyDiscountCommand(sessionId, Percent: null, Amount: 20m, "ajuste", "SESSION", null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Session.DiscountPercent.Should().Be(20m);
        result.Value.Session.Discount.Should().Be(20m);
    }

    /// <summary>US-054 §7: o contrato aceita percentual OU valor absoluto; informar ambos deve ser recusado para não ignorar valor silenciosamente.</summary>
    [Fact]
    public async Task Desconto_Com_Percentual_E_Valor_Ao_Mesmo_Tempo_E_Recusado()
    {
        var world = await SeedWorldAsync();
        await SeedDiscountLimitAsync(world.TenantId, 50m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Ambígua", unitPrice: 100m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var result = await sender.Send(new ApplyDiscountCommand(sessionId, Percent: 10m, Amount: 5m, "ambíguo", "SESSION", null, null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.ValidationError);
    }

    /// <summary>US-054 §4, cenário "Desconto por item": reduz só aquele item, os demais mantêm o preço.</summary>
    [Fact]
    public async Task Desconto_Por_Item_Afeta_Apenas_Aquele_Item()
    {
        var world = await SeedWorldAsync();
        await SeedDiscountLimitAsync(world.TenantId, 50m);
        var pizza = await SeedProductAsync(world.TenantId, "Pizza com Problema", unitPrice: 60m);
        var refrigerante = await SeedProductAsync(world.TenantId, "Refrigerante", unitPrice: 10m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        var pizzaItem = await sender.Send(new AddOrderItemCommand(sessionId, pizza, 1, null, null, null));
        var refriItem = await sender.Send(new AddOrderItemCommand(sessionId, refrigerante, 1, null, null, null));

        var result = await sender.Send(new ApplyDiscountCommand(
            sessionId, Percent: null, Amount: 15m, "qualidade abaixo do esperado", "ITEM", pizzaItem.Value!.Id, null));

        result.IsSuccess.Should().BeTrue();

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var pizzaStored = await verifyDb.OrderItems.SingleAsync(i => i.Id == pizzaItem.Value.Id);
        var refriStored = await verifyDb.OrderItems.SingleAsync(i => i.Id == refriItem.Value!.Id);
        pizzaStored.TotalPrice.Should().Be(45m, "60 - 15 de desconto");
        refriStored.TotalPrice.Should().Be(10m, "item sem desconto mantém o preço original");
        refriStored.Discount.Should().Be(0m);
    }

    /// <summary>US-053 §4, cenário "Retirada registrada": taxa some do total, evento e auditoria com motivo e autor.</summary>
    [Fact]
    public async Task Retirada_Full_Da_Taxa_Zera_A_Taxa_E_Registra_Auditoria()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 10m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Família", unitPrice: 180m);
        var sessionId = await OpenSessionAsync(world);

        var caixa = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: caixa);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var result = await sender.Send(new WaiveSessionServiceFeeCommand(sessionId, "Cliente não concordou com a taxa", "FULL"));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Session.ServiceFee.Should().Be(0m);
        result.Value.Session.Total.Should().Be(180m);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.ServiceFeeWaived.Should().BeTrue();

        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.Action == "SERVICE_FEE_WAIVED" && a.EntityId == sessionId);
        audit.ActorId.Should().Be(caixa);
        audit.Reason.Should().Be("Cliente não concordou com a taxa");

        var events = await verifyDb.DomainEvents.Where(e => e.Type == "service_fee.waived" && e.AggregateId == sessionId).ToListAsync();
        events.Should().HaveCount(1);
    }

    /// <summary>US-053 §4/RN-010: retirada de taxa precisa de motivo para a trilha de auditoria ser útil.</summary>
    [Fact]
    public async Task Retirada_Da_Taxa_Sem_Motivo_E_Recusada()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 10m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Sem Motivo", unitPrice: 100m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var result = await sender.Send(new WaiveSessionServiceFeeCommand(sessionId, "", "FULL"));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.ValidationError);
    }

    /// <summary>US-053 §4, cenário "Padrão anômalo de retirada": mesmo garçom retira a taxa em 80%+ das contas do dia -> alerta ao gestor.</summary>
    [Fact]
    public async Task Retirada_Em_Padrao_Anomalo_Dispara_Alerta_Ao_Gestor()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 10m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza do Garçom", unitPrice: 50m);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        // 3 contas do MESMO garçom no mesmo dia, todas com a taxa retirada -> 100% >= 80% (limiar).
        for (var i = 0; i < 3; i++)
        {
            var sessionId = await OpenSessionAsync(world, tableLabel: $"A{i}", waiterId: world.WaiterId);
            await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));
            var waive = await sender.Send(new WaiveSessionServiceFeeCommand(sessionId, "motivo recorrente", "FULL"));
            waive.IsSuccess.Should().BeTrue();
        }

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var alert = await verifyDb.Alerts.SingleOrDefaultAsync(a => a.Type == AlertTypes.ServiceFeeWaiveAboveThreshold);
        alert.Should().NotBeNull("3 de 3 contas do mesmo garçom com taxa retirada deveria disparar o alerta de padrão anômalo");
        alert!.TargetRoles.Should().Contain("MANAGER");
    }

    private sealed record World(Guid TenantId, Guid StoreId, Guid AreaId, Guid WaiterId, Guid DeviceId);

    private async Task<World> SeedWorldAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var waiterId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        await using var storeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        storeDb.Stores.Add(Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));
        storeDb.Areas.Add(Area.Create(tenantId, storeId, "Salão de teste"));
        var device = Device.Create(tenantId, storeId, "Terminal de teste", DeviceType.Waiter, $"fingerprint-{Guid.NewGuid():N}");
        storeDb.Devices.Add(device);
        await storeDb.SaveChangesAsync();

        return new World(tenantId, storeId, (await storeDb.Areas.SingleAsync()).Id, waiterId, device.Id);
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

    private async Task SeedDiscountLimitAsync(Guid tenantId, decimal maxWithoutAuthPercent)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var config = await db.TenantConfigs.SingleOrDefaultAsync(c => c.TenantId == tenantId);
        if (config is null)
        {
            config = TenantConfig.Create(tenantId);
            db.TenantConfigs.Add(config);
        }

        config.UpdateOperation($"{{\"maxDiscountWithoutAuthPercent\": {maxWithoutAuthPercent.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedManagerWithPermissionAsync(World world, string pin, string permission)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var hasher = new Argon2CredentialHasher();
        var pinDigester = new HmacPinLookupDigester(Options.Create(new AuthSecretsOptions { PinLookupPepper = TestPinLookupPepper }));

        var role = Role.Create(world.TenantId, "MANAGER", "Gerente de teste", isSystem: true);
        role.UpdatePermissions($"[\"{permission}\"]");
        db.Roles.Add(role);

        var manager = AppUser.Create(
            world.TenantId, "Gerente de teste", email: null, passwordHash: null, pinHash: hasher.Hash(pin), pinLookup: pinDigester.Digest(pin));
        db.Users.Add(manager);
        db.UserRoles.Add(UserRole.Create(world.TenantId, manager.Id, role.Id));

        await db.SaveChangesAsync();
        return manager.Id;
    }

    private async Task<Guid> OpenSessionAsync(World world, string tableLabel = "1", Guid? waiterId = null)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var uniqueLabel = $"{tableLabel}-{Guid.NewGuid():N}"[..12];
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, uniqueLabel, $"qr-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2,
            waiterId: waiterId, openedSource: "WAITER");
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
        if (db is Nexora.Infrastructure.Persistence.AppDbContext appDbContext)
        {
            services.AddSingleton<IOrderShortCodeAllocator>(new OrderShortCodeAllocator(appDbContext));
        }

        // Auth (ADR-023) — pilha REAL (mesma de produção), necessária para o cenário "desconto
        // acima do limite com autorização" (US-054 §4). Mesmo padrão de PendingItemsOnCloseIntegrationTests.
        services.AddSingleton<ICredentialHasher, Argon2CredentialHasher>();
        services.AddSingleton<IPinLookupDigester>(new HmacPinLookupDigester(Options.Create(new AuthSecretsOptions { PinLookupPepper = TestPinLookupPepper })));
        services.AddSingleton(Options.Create(new JwtOptions { Secret = TestJwtSecret }));
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IAuthorizationTokenValidator, Nexora.Application.Auth.Shared.AuthorizationTokenValidator>();

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
