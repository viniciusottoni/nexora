using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Commands.AuthorizeSensitiveAction;
using Nexora.Application.Tables.Commands.RegisterPartialPayment;
using Nexora.Application.Tables.Commands.RequestBill;
using Nexora.Application.Tables.Support;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Auth;
using Nexora.Infrastructure.Devices;
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
/// Cenários Gherkin da US-035 (Bloquear fechamento com item pendente) contra um PostgreSQL real
/// (Testcontainers), mesmo pipeline MediatR de produção (ADR-037). Arquivo NOVO com container
/// PRÓPRIO (mesmo espírito de <c>CancelOrderIntegrationTests</c>, US-033): a checagem exige a
/// pilha REAL de autorização pontual (ADR-023) — <c>AuthorizeSensitiveActionCommand</c> com PIN
/// real do gerente — e não reaproveita <see cref="MediatRTestContainerFactory"/> para não competir
/// por edição em paralelo com outros agentes/histórias naquele arquivo compartilhado.
///
/// Cobre os dois pontos de "fechamento" já existentes hoje (na ausência de US-052, ver decisão de
/// escopo desta história): <c>RequestBillCommand</c> (transição para BILL_REQUESTED) e
/// <c>RegisterPartialPaymentCommand</c> (pagamento parcial) — os três modos configuráveis
/// (<c>BLOCK</c>/<c>WARN</c>/<c>IGNORE</c>), a autorização registrando autor+autorizador+motivo, e
/// o cancelamento do item pendente liberando o fechamento sem novo bloqueio.
/// </summary>
[Collection("Postgres")]
public sealed class PendingItemsOnCloseIntegrationTests
{
    private const string TestJwtSecret = "pending-items-integration-test-jwt-secret-32-bytes!!";
    private const string TestPinLookupPepper = "pending-items-integration-test-pin-lookup-pepper-32b!!";

    private readonly PostgresFixture _fixture;

    public PendingItemsOnCloseIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Fechamento bloqueado" (US-035 §4): item READY ainda não entregue bloqueia a solicitação da conta.</summary>
    [Fact]
    public async Task RequestBill_Modo_Block_Sem_Autorizacao_E_Recusado_Com_Itens_Pendentes()
    {
        var world = await SeedWorldAsync();
        await SeedPendingItemsModeAsync(world.TenantId, PendingItemsClosePolicy.Block);
        var variantId = await SeedProductAsync(world.TenantId, "Petit Gateau");
        var sessionId = await OpenSessionAsync(world);
        await SeedOrderWithSingleItemAsync(world, sessionId, variantId, OrderItemStatus.Ready);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RequestBillCommand(sessionId, "SINGLE", null));

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.PendingItems);
        result.Errors.Should().ContainKey(PendingItemsClosePolicy.MetaErrorsKey);
        result.Errors![PendingItemsClosePolicy.MetaErrorsKey].Single().Should().Contain("Petit Gateau").And.Contain("READY");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.Status.Should().Be(TableSessionStatus.Open, "a recusa não muda o estado da sessão");
    }

    /// <summary>Cenário Gherkin "Fechamento autorizado mesmo com pendência" (US-035 §4).</summary>
    [Fact]
    public async Task RequestBill_Modo_Block_Autorizado_Prossegue_E_Audita_Autor_Autorizador_E_Motivo()
    {
        var world = await SeedWorldAsync();
        await SeedPendingItemsModeAsync(world.TenantId, PendingItemsClosePolicy.Block);
        const string managerPin = "7788";
        var managerId = await SeedManagerWithPermissionAsync(world, managerPin, "order:close_with_pending");
        var variantId = await SeedProductAsync(world.TenantId, "Sobremesa");
        var sessionId = await OpenSessionAsync(world);
        await SeedOrderWithSingleItemAsync(world, sessionId, variantId, OrderItemStatus.Queued);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var authorized = await sender.Send(new AuthorizeSensitiveActionCommand(
            "CLOSE_WITH_PENDING", managerPin, new Dictionary<string, object?> { ["sessionId"] = sessionId.ToString() }));
        authorized.IsSuccess.Should().BeTrue(authorized.IsFailure ? authorized.Code : string.Empty);
        authorized.Value!.AuthorizedBy.Id.Should().Be(managerId);

        var result = await sender.Send(new RequestBillCommand(
            sessionId, "SINGLE", null, authorized.Value.AuthorizationToken, "Cliente desistiu do item"));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Session.Status.Should().Be("BILLREQUESTED");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.Action == "CLOSE_WITH_PENDING" && a.EntityId == sessionId);
        audit.ActorId.Should().Be(world.WaiterId, "executor — o operador que estava na tela");
        audit.AuthorizedBy.Should().Be(managerId, "autorizador — o gerente que informou o PIN");
        audit.Reason.Should().Be("Cliente desistiu do item");

        var session = await verifyDb.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.Status.Should().Be(TableSessionStatus.BillRequested);
    }

    /// <summary>Cenário Gherkin "Comportamento configurável" (US-035 §4): modo WARN prossegue sem autorização e lista os pendentes.</summary>
    [Fact]
    public async Task RequestBill_Modo_Warn_Prossegue_Sem_Autorizacao_E_Devolve_Os_Pendentes_Para_Aviso()
    {
        var world = await SeedWorldAsync();
        await SeedPendingItemsModeAsync(world.TenantId, PendingItemsClosePolicy.Warn);
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Marguerita");
        var sessionId = await OpenSessionAsync(world);
        await SeedOrderWithSingleItemAsync(world, sessionId, variantId, OrderItemStatus.Queued);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RequestBillCommand(sessionId, "SINGLE", null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.Session.Status.Should().Be("BILLREQUESTED");
        result.Value.PendingItems.Should().ContainSingle(p => p.Name.Contains("Pizza Marguerita") && p.Status == "QUEUED");
    }

    /// <summary>Modo IGNORE (US-035 §3.1/§8): prossegue silenciosamente — nem a lista de pendentes é exposta.</summary>
    [Fact]
    public async Task RequestBill_Modo_Ignore_Prossegue_Silenciosamente_Sem_Expor_Pendentes()
    {
        var world = await SeedWorldAsync();
        await SeedPendingItemsModeAsync(world.TenantId, PendingItemsClosePolicy.Ignore);
        var variantId = await SeedProductAsync(world.TenantId, "Suco");
        var sessionId = await OpenSessionAsync(world);
        await SeedOrderWithSingleItemAsync(world, sessionId, variantId, OrderItemStatus.Queued);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RequestBillCommand(sessionId, "SINGLE", null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.PendingItems.Should().BeEmpty("IGNORE nem avisa o caixa da pendência");
    }

    /// <summary>Cenário Gherkin "Item cancelado resolve a pendência" (US-035 §4).</summary>
    [Fact]
    public async Task RequestBill_Item_Cancelado_Libera_O_Fechamento_Sem_Novo_Bloqueio()
    {
        var world = await SeedWorldAsync();
        await SeedPendingItemsModeAsync(world.TenantId, PendingItemsClosePolicy.Block);
        var variantId = await SeedProductAsync(world.TenantId, "Torta de Limão");
        var sessionId = await OpenSessionAsync(world);
        var itemId = await SeedOrderWithSingleItemAsync(world, sessionId, variantId, OrderItemStatus.Queued);

        // Cliente desistiu do item — cancelado diretamente no domínio (cancelamento em si é US-033,
        // fora desta história; aqui só se prova que a pendência deixa de bloquear depois disso).
        await using (var cancelDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var item = await cancelDb.OrderItems.SingleAsync(i => i.Id == itemId);
            item.Cancel("Cliente desistiu", world.WaiterId);
            await cancelDb.SaveChangesAsync();
        }

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RequestBillCommand(sessionId, "SINGLE", null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty, "o único item pendente foi cancelado — nada mais bloqueia");
        result.Value!.Session.Status.Should().Be("BILLREQUESTED");
    }

    /// <summary>Mesma regra (US-035, ver decisão de escopo) aplicada ao OUTRO ponto de "fechamento": o pagamento parcial.</summary>
    [Fact]
    public async Task RegisterPartialPayment_Modo_Block_Sem_Autorizacao_E_Recusado_Com_Itens_Pendentes()
    {
        var world = await SeedWorldAsync();
        // WARN no momento de pedir a conta — a pendência não impede chegar a BILL_REQUESTED; o
        // item continua não entregue quando o caixa tenta o pagamento a seguir.
        await SeedPendingItemsModeAsync(world.TenantId, PendingItemsClosePolicy.Warn);
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Calabresa");
        var sessionId = await OpenSessionAsync(world);
        await SeedOrderWithSingleItemAsync(world, sessionId, variantId, OrderItemStatus.Queued);
        await RequestBillDirectlyAsync(world, sessionId);

        await SeedPendingItemsModeAsync(world.TenantId, PendingItemsClosePolicy.Block);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RegisterPartialPaymentCommand(sessionId, 10m, "CASH"));

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.PendingItems);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        (await verifyDb.Payments.Where(p => p.SessionId == sessionId).ToListAsync()).Should().BeEmpty("nenhum pagamento deve ser registrado quando bloqueado");
    }

    /// <summary>Autorização válida no pagamento parcial também prossegue e audita autor+autorizador+motivo.</summary>
    [Fact]
    public async Task RegisterPartialPayment_Modo_Block_Autorizado_Prossegue_E_Audita()
    {
        var world = await SeedWorldAsync();
        const string managerPin = "3344";
        var managerId = await SeedManagerWithPermissionAsync(world, managerPin, "order:close_with_pending");
        await SeedPendingItemsModeAsync(world.TenantId, PendingItemsClosePolicy.Warn);
        var variantId = await SeedProductAsync(world.TenantId, "Pizza Quatro Queijos");
        var sessionId = await OpenSessionAsync(world);
        await SeedOrderWithSingleItemAsync(world, sessionId, variantId, OrderItemStatus.Queued);
        await RequestBillDirectlyAsync(world, sessionId);

        await SeedPendingItemsModeAsync(world.TenantId, PendingItemsClosePolicy.Block);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, userId: world.WaiterId, deviceId: world.DeviceId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var authorized = await sender.Send(new AuthorizeSensitiveActionCommand(
            "CLOSE_WITH_PENDING", managerPin, new Dictionary<string, object?> { ["sessionId"] = sessionId.ToString() }));
        authorized.IsSuccess.Should().BeTrue(authorized.IsFailure ? authorized.Code : string.Empty);

        var result = await sender.Send(new RegisterPartialPaymentCommand(
            sessionId, 10m, "CASH", authorized.Value!.AuthorizationToken, "Autorizado pelo gerente no balcão"));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.AmountPaid.Should().Be(10m);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.Action == "CLOSE_WITH_PENDING" && a.EntityId == sessionId);
        audit.ActorId.Should().Be(world.WaiterId);
        audit.AuthorizedBy.Should().Be(managerId);
        audit.Reason.Should().Be("Autorizado pelo gerente no balcão");
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
        // Device REAL (não um Guid solto) — AuthorizeSensitiveActionCommandHandler grava
        // AuthAttempt.DeviceId, e auth_attempt tem FK para devices.
        var device = Device.Create(tenantId, storeId, "Terminal de teste", DeviceType.Waiter, $"fingerprint-{Guid.NewGuid():N}");
        storeDb.Devices.Add(device);
        await storeDb.SaveChangesAsync();

        return new World(tenantId, storeId, (await storeDb.Areas.SingleAsync()).Id, waiterId, device.Id);
    }

    private async Task SeedPendingItemsModeAsync(Guid tenantId, string mode)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var config = await db.TenantConfigs.SingleOrDefaultAsync(c => c.TenantId == tenantId);
        if (config is null)
        {
            config = TenantConfig.Create(tenantId);
            db.TenantConfigs.Add(config);
        }

        config.UpdateOperation($$"""{"pendingItemsOnClose": "{{mode}}"}""");
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Gerente com PIN real (Argon2, mesmo algoritmo de produção) e a permissão elevável de
    /// <paramref name="permission"/> — habilita o fluxo completo de
    /// <c>AuthorizeSensitiveActionCommand</c>. Mesmo pepper de PIN configurado em
    /// <see cref="BuildContainer"/>, para o lookup bater.
    /// </summary>
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

    private async Task<Guid> SeedProductAsync(Guid tenantId, string productName)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var category = Category.Create(tenantId, "Categoria de teste");
        db.Categories.Add(category);

        var product = Product.Create(tenantId, category.Id, productName);
        db.Products.Add(product);

        var variant = ProductVariant.Create(tenantId, product.Id, "Único");
        db.ProductVariants.Add(variant);

        db.Prices.Add(Price.Create(tenantId, variant.Id, Channel.DineIn, 20m));

        await db.SaveChangesAsync();

        return variant.Id;
    }

    private async Task<Guid> OpenSessionAsync(World world)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var uniqueLabel = $"T-{Guid.NewGuid():N}"[..12];
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, uniqueLabel, $"qr-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2, waiterId: world.WaiterId, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();

        return session.Id;
    }

    /// <summary>
    /// Cria um pedido PLACED com um único item já avançado até <paramref name="itemStatus"/> —
    /// direto no domínio (não via <c>AddOrderItemCommand</c>, que depende de broadcasters de US-030/
    /// US-031 fora do escopo desta suíte), mesmo espírito de <c>CancelOrderIntegrationTests</c>.
    /// </summary>
    private async Task<Guid> SeedOrderWithSingleItemAsync(World world, Guid sessionId, Guid variantId, OrderItemStatus itemStatus)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var order = Order.Create(world.TenantId, world.StoreId, Channel.DineIn, ShortCode(), DateOnly.FromDateTime(DateTime.UtcNow), sessionId: sessionId);
        order.Place();

        var item = OrderItem.Create(world.TenantId, order.Id, variantId, unitPrice: 20m);
        AdvanceItemTo(item, itemStatus);
        order.AddItem(item);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return item.Id;
    }

    /// <summary>
    /// Move a sessão para BILL_REQUESTED sem passar pelo comando completo (mesmo espírito de
    /// <c>BillSplitIntegrationTests.RequestBillDirectlyAsync</c>) — usado quando o teste quer isolar
    /// o comportamento do PAGAMENTO parcial, não o da solicitação da conta em si.
    /// </summary>
    private async Task RequestBillDirectlyAsync(World world, Guid sessionId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await db.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.RequestBill("SINGLE", null);
        await db.SaveChangesAsync();
    }

    private static void AdvanceItemTo(OrderItem item, OrderItemStatus target)
    {
        if (target == OrderItemStatus.Queued) return;
        item.Fire(Guid.NewGuid());
        if (target == OrderItemStatus.Fired) return;
        item.SendToOven(ovenSlot: null);
        if (target == OrderItemStatus.InOven) return;
        item.TakeOutOfOven();
        if (target == OrderItemStatus.OutOfOven) return;
        item.MarkReady(Guid.NewGuid());
        if (target == OrderItemStatus.Ready) return;
        item.MarkServed(Guid.NewGuid());
    }

    private static string ShortCode() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static ServiceProvider BuildContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton<IAlertsBroadcaster>(new RecordingAlertsBroadcaster());
        services.AddSingleton<ITableMapBroadcaster>(new RecordingTableMapBroadcaster());

        // Auth (ADR-023) — pilha REAL (mesma de produção) para exercitar o fluxo completo de
        // AuthorizeSensitiveActionCommand -> RequestBillCommand/RegisterPartialPaymentCommand de
        // ponta a ponta, sem duplo nenhum de infraestrutura de segurança.
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
