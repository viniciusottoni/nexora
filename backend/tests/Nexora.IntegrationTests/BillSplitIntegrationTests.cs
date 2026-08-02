using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Tables.Commands.AssignBillItems;
using Nexora.Application.Tables.Commands.RegisterPartialPayment;
using Nexora.Application.Tables.Commands.WaiveServiceFee;
using Nexora.Application.Tables.Queries.GetBill;
using Nexora.Application.Tables.Queries.GetCurrentSessionBill;
using Nexora.Contracts.Operation;
using Nexora.Domain.Cashier;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Devices;
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

/// <summary>
/// Cenários Gherkin da US-027 (Dividir a conta) contra um PostgreSQL real (Testcontainers) — mesmo
/// pipeline MediatR de produção (ADR-037). Cobre especificamente os dois cenários de integração
/// exigidos pela estratégia de teste da história (US-027 §12): "Divisão por item recusa conclusão
/// com item não atribuído" e "Pagamento parcial mantém a sessão em aberto com o saldo correto" —
/// mais os cenários adjacentes (retirada de taxa auditada, prévia pública).
/// </summary>
[Collection("Postgres")]
public sealed class BillSplitIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public BillSplitIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Divisão por item" (US-027 §4): nenhum item pode ficar sem atribuição antes de fechar.</summary>
    [Fact]
    public async Task Divisao_Por_Item_Recusa_Conclusao_Com_Item_Nao_Atribuido()
    {
        var world = await SeedWorldAsync();
        var pizza = await SeedProductAsync(world.TenantId, "Pizza Marguerita", "Média", 40m);
        var refrigerante = await SeedProductAsync(world.TenantId, "Refrigerante", "Lata", 8m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var itemA = await sender.Send(new AddOrderItemCommand(sessionId, pizza, 1, null, null, null));
        var itemB = await sender.Send(new AddOrderItemCommand(sessionId, refrigerante, 1, null, null, null));
        itemA.IsSuccess.Should().BeTrue();
        itemB.IsSuccess.Should().BeTrue();

        // Só o item A foi atribuído — o item B (refrigerante) fica órfão.
        var assignments = new[] { new BillItemAssignmentInput(1, new[] { itemA.Value!.Id }) };
        var result = await sender.Send(new AssignBillItemsCommand(sessionId, assignments, ServiceFeeWaivedPersons: null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.BillItemNotAssigned);
        result.Errors.Should().NotBeNull();
        result.Errors!["itemIds"].Should().Contain(itemB.Value!.Id.ToString());
    }

    /// <summary>Contraprova do teste acima: com todos os itens atribuídos, a divisão por item é aceita e cada parte contém só os itens atribuídos.</summary>
    [Fact]
    public async Task Divisao_Por_Item_Com_Todos_Os_Itens_Atribuidos_E_Aceita()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m); // isola a asserção da distribuição de itens da taxa de serviço (testada à parte)
        var pizza = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Grande", 50m);
        var sobremesa = await SeedProductAsync(world.TenantId, "Sobremesa", "Individual", 20m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var itemA = await sender.Send(new AddOrderItemCommand(sessionId, pizza, 1, null, null, null));
        var itemB = await sender.Send(new AddOrderItemCommand(sessionId, sobremesa, 1, null, null, null));

        var assignments = new[]
        {
            new BillItemAssignmentInput(1, new[] { itemA.Value!.Id }),
            new BillItemAssignmentInput(2, new[] { itemB.Value!.Id }),
        };
        var result = await sender.Send(new AssignBillItemsCommand(sessionId, assignments, ServiceFeeWaivedPersons: null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnassignedItemIds.Should().BeEmpty();
        result.Value.Split.Should().HaveCount(2);
        result.Value.Split.Single(p => p.Person == 1).Amount.Should().Be(50m);
        result.Value.Split.Single(p => p.Person == 2).Amount.Should().Be(20m);
        result.Value.Split.Sum(p => p.Amount).Should().Be(result.Value.Total);
    }

    /// <summary>Cenário Gherkin "Divisão por valor" (US-027 §4): R$50 pagos de R$180 deixam R$130 em aberto e a sessão continua BILL_REQUESTED.</summary>
    [Fact]
    public async Task Pagamento_Parcial_Mantem_A_Sessao_Em_Aberto_Com_O_Saldo_Correto()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m); // reproduz os valores exatos do Gherkin (US-027 §4), sem taxa de serviço no meio da conta
        var produto = await SeedProductAsync(world.TenantId, "Pizza Portuguesa", "Grande", unitPrice: 180m);
        var sessionId = await OpenSessionAsync(world);

        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        await using (var provider = BuildContainer(db, new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var added = await provider.GetRequiredService<ISender>().Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));
            added.IsSuccess.Should().BeTrue();
        }

        await RequestBillDirectlyAsync(world, sessionId, "BY_AMOUNT", null);

        // Novo DbContext/provider — o `db` acima já teria a TableSession em cache (Open) se ela
        // tivesse sido consultada antes; usar uma instância nova é o que qualquer requisição HTTP
        // real faria (escopo de DI por requisição), e evita reproduzir aqui o mesmo cuidado que
        // OrderConsumptionIntegrationTests já documenta para esse tipo de escrita "fora de banda".
        await using var paymentDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        await using var paymentProvider = BuildContainer(paymentDb, new StaticTenantContext(world.TenantId, world.StoreId));
        var payment = await paymentProvider.GetRequiredService<ISender>().Send(new RegisterPartialPaymentCommand(sessionId, 50m, "CASH"));

        payment.IsSuccess.Should().BeTrue();
        payment.Value!.AmountPaid.Should().Be(50m);
        payment.Value.RemainingAmount.Should().Be(130m, "R$180 - R$50 pagos = R$130 em aberto (sem taxa de serviço configurada)");
        payment.Value.SessionStatus.Should().Be("BILLREQUESTED");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.Status.Should().Be(TableSessionStatus.BillRequested, "US-027: a sessão permanece em BILL_REQUESTED após pagamento parcial");

        var storedPayment = await verifyDb.Payments.SingleAsync(p => p.Id == payment.Value.PaymentId);
        storedPayment.SessionId.Should().Be(sessionId);
        storedPayment.Amount.Should().Be(50m);
        storedPayment.Status.Should().Be(PaymentStatus.Paid);
    }

    /// <summary>Uma segunda consulta reflete o saldo já reduzido pelo primeiro pagamento parcial.</summary>
    [Fact]
    public async Task Segundo_Pagamento_Parcial_Considera_O_Saldo_Ja_Reduzido()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Frango com Catupiry", "Grande", unitPrice: 100m);
        var sessionId = await OpenSessionAsync(world);

        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        await using (var provider = BuildContainer(db, new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            await provider.GetRequiredService<ISender>().Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));
        }

        await RequestBillDirectlyAsync(world, sessionId, "BY_AMOUNT", null);

        // Cada pagamento usa um DbContext/provider novo (ver comentário do teste anterior) — cada
        // um enxerga o saldo já reduzido pelo pagamento anterior, exatamente como uma nova
        // requisição HTTP enxergaria.
        async Task<Result<PartialPaymentResponse>> SendAsync(decimal amount, string method)
        {
            await using var paymentDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
            await using var paymentProvider = BuildContainer(paymentDb, new StaticTenantContext(world.TenantId, world.StoreId));
            return await paymentProvider.GetRequiredService<ISender>().Send(new RegisterPartialPaymentCommand(sessionId, amount, method));
        }

        var first = await SendAsync(40m, "PIX");
        first.IsSuccess.Should().BeTrue();
        first.Value!.RemainingAmount.Should().Be(60m);

        var second = await SendAsync(60m, "CASH");
        second.IsSuccess.Should().BeTrue();
        second.Value!.RemainingAmount.Should().Be(0m);

        // Terceiro pagamento não cabe mais no saldo — recusado com código estável.
        var third = await SendAsync(0.01m, "CASH");
        third.IsSuccess.Should().BeFalse();
        third.Code.Should().Be(ApiErrorCodes.BillInvalidAmount);
    }

    /// <summary>Pagamento parcial pedido antes de a conta ser solicitada é recusado com código estável.</summary>
    [Fact]
    public async Task Pagamento_Parcial_Antes_De_Solicitar_A_Conta_E_Recusado()
    {
        var world = await SeedWorldAsync();
        var produto = await SeedProductAsync(world.TenantId, "Suco", "500ml", unitPrice: 9m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var result = await sender.Send(new RegisterPartialPaymentCommand(sessionId, 5m, "CASH"));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.BillNotRequested);
    }

    /// <summary>Cenário Gherkin "Retirada da taxa por uma das partes" (US-027 §4): só a parte de quem retirou muda, e a retirada é auditada com autor.</summary>
    [Fact]
    public async Task Retirada_Da_Taxa_Recalcula_So_A_Parte_E_Fica_Auditada_Com_Autor()
    {
        var world = await SeedWorldAsync();
        var produto = await SeedProductAsync(world.TenantId, "Pizza Quatro Queijos", "Grande", unitPrice: 100m);
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 10m);
        var sessionId = await OpenSessionAsync(world);
        var caixa = Guid.NewGuid();

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        await provider.GetRequiredService<ISender>().Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var actorContext = new StaticTenantContext(world.TenantId, world.StoreId, caixa);
        await using var actorDb = _fixture.CreateAppDbContext(actorContext);
        await using var actorProvider = BuildContainer(actorDb, actorContext);

        var result = await actorProvider.GetRequiredService<ISender>().Send(
            new WaiveServiceFeeCommand(sessionId, People: 4, Person: 2, AlreadyWaivedPersons: null, Reason: "Cliente não concordou com a taxa"));

        result.IsSuccess.Should().BeTrue();
        var pessoaDois = result.Value!.Split.Single(p => p.Person == 2);
        pessoaDois.ServiceFeeWaived.Should().BeTrue();
        pessoaDois.ServiceFeeAmount.Should().Be(0m);
        result.Value.Split.Where(p => p.Person != 2).Should().OnlyContain(p => !p.ServiceFeeWaived);
        result.Value.Split.Sum(p => p.Amount).Should().Be(result.Value.Total);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.EntityId == sessionId && a.Action == "TABLE_SESSION_SERVICE_FEE_WAIVED");
        audit.ActorId.Should().Be(caixa, "RN-010: a retirada é registrada com autor");
        audit.Reason.Should().Be("Cliente não concordou com a taxa");
    }

    /// <summary>US-027 §10: a prévia pública (cliente) usa a MESMA sessão do token — nunca um id informado pelo chamador.</summary>
    [Fact]
    public async Task Previa_Publica_Calcula_A_Mesma_Divisao_Da_Sessao_Corrente()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Napolitana", "Grande", unitPrice: 90m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        await provider.GetRequiredService<ISender>().Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var publicContext = new StaticTenantContext(world.TenantId, world.StoreId, sessionId: sessionId);
        await using var publicDb = _fixture.CreateAppDbContext(publicContext);
        await using var publicProvider = BuildContainer(publicDb, publicContext);

        var result = await publicProvider.GetRequiredService<ISender>().Send(
            new GetCurrentSessionBillQuery("BY_PERSON", 3, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Split.Should().HaveCount(3);
        result.Value.Split.Sum(p => p.Amount).Should().Be(result.Value.Total);
        result.Value.Total.Should().Be(90m);
    }

    /// <summary>GET /v1/sessions/{id}/bill usa a preferência de SplitMode/SplitPeople da sessão quando a query não sobrepõe.</summary>
    [Fact]
    public async Task Consulta_Da_Conta_Usa_Preferencia_Da_Sessao_Quando_Query_Nao_Sobrepoe()
    {
        var world = await SeedWorldAsync();
        var produto = await SeedProductAsync(world.TenantId, "Pizza Vegetariana", "Média", unitPrice: 60m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        await provider.GetRequiredService<ISender>().Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        await RequestBillDirectlyAsync(world, sessionId, "BY_PERSON", 4);

        var result = await provider.GetRequiredService<ISender>().Send(new GetBillQuery(sessionId, null, null, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.SplitMode.Should().Be("BY_PERSON");
        result.Value.Split.Should().HaveCount(4, "a sessão pediu a conta para 4 pessoas (US-026) e a query não sobrepôs esse valor");
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
        var store = Domain.Platform.Store.Create(storeId, tenantId, "Loja de teste", isDefault: true);
        storeDb.Stores.Add(store);
        var area = Area.Create(tenantId, storeId, "Salão de teste");
        storeDb.Areas.Add(area);
        await storeDb.SaveChangesAsync();

        return new World(tenantId, storeId, area.Id);
    }

    private async Task<Guid> SeedProductAsync(Guid tenantId, string productName, string variantName, decimal unitPrice)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var category = Category.Create(tenantId, "Categoria de teste");
        db.Categories.Add(category);

        var product = Product.Create(tenantId, category.Id, productName);
        db.Products.Add(product);

        var variant = ProductVariant.Create(tenantId, product.Id, variantName);
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

    private async Task<Guid> OpenSessionAsync(World world, string tableLabel = "1", string qrToken = "qr-mesa-1")
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var uniqueLabel = $"{tableLabel}-{Guid.NewGuid():N}"[..12];
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, uniqueLabel, $"{qrToken}-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();

        return session.Id;
    }

    /// <summary>
    /// Move a sessão para BILL_REQUESTED sem passar pelo comando completo (que dispara alertas
    /// alheios a este teste) — mesmo espírito de outros testes desta suíte que escrevem "fora de
    /// banda" antes de reagir ao estado gravado. <paramref name="world"/> resolve o tenant certo
    /// para o contexto RLS (ADR-004) — nunca <c>null</c>, que não enxergaria nenhuma linha.
    /// </summary>
    private async Task RequestBillDirectlyAsync(World world, Guid sessionId, string splitMode, short? people)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await db.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.RequestBill(splitMode, people);
        await db.SaveChangesAsync();
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
