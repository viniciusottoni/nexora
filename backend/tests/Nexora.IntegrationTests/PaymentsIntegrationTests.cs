using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Cashier.Commands.RegisterPayments;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;
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

/// <summary>
/// Cenários Gherkin de US-052 (Múltiplas formas de pagamento na mesma conta) e US-058 (Registrar
/// pagamento de maquininha externa) contra um PostgreSQL real (Testcontainers) — mesmo pipeline
/// MediatR de produção (ADR-037), mesmo padrão de setup de <c>BillSplitIntegrationTests</c>.
/// </summary>
[Collection("Postgres")]
public sealed class PaymentsIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public PaymentsIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>US-052 §4, cenário "Três formas na mesma conta": conta de R$198 quitada em crédito+PIX+dinheiro, sessão vai para PAID e depois CLOSED, mesa liberada.</summary>
    [Fact]
    public async Task Tres_Formas_Na_Mesma_Conta_Fecha_A_Sessao_E_Libera_A_Mesa()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 10m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Grande", "Única", unitPrice: 180m);
        var (sessionId, tableId) = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        // subtotal 180 + taxa 10% (18) = total 198 — bate exatamente com o cenário Gherkin da US.
        var payments = new List<PaymentInput>
        {
            new("CREDIT", 100m, null, null, null, null, 1, false),
            new("PIX", 50m, null, null, null, null, 1, false),
            new("CASH", 48m, 48m, null, null, null, 1, false),
        };

        var result = await sender.Send(new RegisterPaymentsCommand(sessionId, payments));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Session.Status.Should().Be("CLOSED");
        result.Value.Payments.Should().HaveCount(3);
        result.Value.Change.Should().Be(0m);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var session = await verifyDb.TableSessions.SingleAsync(s => s.Id == sessionId);
        session.Status.Should().Be(TableSessionStatus.Closed);
        session.TotalAmount.Should().Be(198m);
        session.ReleasedAt.Should().NotBeNull();

        var table = await verifyDb.DiningTables.SingleAsync(t => t.Id == tableId);
        table.Status.Should().Be(TableStatus.Free);

        var storedPayments = await verifyDb.Payments.Where(p => p.SessionId == sessionId).ToListAsync();
        storedPayments.Should().HaveCount(3);
        storedPayments.Sum(p => p.Amount).Should().Be(198m);

        var events = await verifyDb.DomainEvents.Where(e => e.AggregateType == "payment" && e.Type == "payment.registered").ToListAsync();
        events.Should().HaveCount(3, "EVT-032 deve ser emitido para CADA pagamento (US-052 §4)");

        var financialEntry = await verifyDb.FinancialEntries.SingleAsync(f => f.ReferenceId == sessionId);
        financialEntry.Amount.Should().Be(198m, "US-052 §4: a receita registrada deve ser o total da conta");
        financialEntry.Type.Should().Be(Domain.Finance.FinancialEntryType.Revenue);
    }

    /// <summary>US-052 §4, cenário "Soma divergente do total": R$190 informados para uma conta de R$198 -> 422 com a diferença exata.</summary>
    [Fact]
    public async Task Soma_Divergente_Do_Total_E_Recusada_Com_A_Diferenca_Exata()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 10m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Média", "Única", unitPrice: 180m);
        var (sessionId, _) = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var payments = new List<PaymentInput> { new("CASH", 190m, 190m, null, null, null, 1, false) };
        var result = await sender.Send(new RegisterPaymentsCommand(sessionId, payments));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.PaymentSumMismatch);
        decimal.Parse(result.Errors!["difference"].Single(), System.Globalization.CultureInfo.InvariantCulture).Should().Be(8m);
    }

    /// <summary>US-052 §4, cenário "Pagamento parcial": a quitação final deve considerar o saldo já pago e fechar só o restante.</summary>
    [Fact]
    public async Task Quitacao_Final_Considera_Pagamento_Parcial_Ja_Registrado()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 10m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Compartilhada", "Única", unitPrice: 180m);
        var (sessionId, _) = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        await using (var partialDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var session = await partialDb.TableSessions.SingleAsync(s => s.Id == sessionId);
            var partialPayment = Payment.Create(
                session.TenantId,
                session.StoreId,
                session.BusinessDay,
                PaymentMethod.Cash,
                amount: 50m,
                netAmount: 50m,
                sessionId: session.Id,
                createdBy: Guid.NewGuid());
            partialPayment.MarkPaid(DateTimeOffset.UtcNow);
            partialDb.Payments.Add(partialPayment);
            await partialDb.SaveChangesAsync();
        }

        var finalPayments = new List<PaymentInput> { new("PIX", 148m, null, null, null, null, 1, false) };
        var result = await sender.Send(new RegisterPaymentsCommand(sessionId, finalPayments));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var sessionStored = await verifyDb.TableSessions.SingleAsync(s => s.Id == sessionId);
        sessionStored.Status.Should().Be(TableSessionStatus.Closed);
        (await verifyDb.Payments.Where(p => p.SessionId == sessionId).SumAsync(p => p.Amount)).Should().Be(198m);
        (await verifyDb.FinancialEntries.SingleAsync(f => f.ReferenceId == sessionId)).Amount.Should().Be(198m);
    }

    /// <summary>US-052 §4, cenário "Troco em dinheiro": R$200 recebidos para uma conta de R$198 geram R$2 de troco, mas a receita registrada é R$198.</summary>
    [Fact]
    public async Task Troco_Em_Dinheiro_E_Calculado_E_Nao_Infla_A_Receita()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 10m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Especial", "Única", unitPrice: 180m);
        var (sessionId, _) = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        // Amount=198 é o valor que quita a conta; ReceivedAmount=200 é o que o cliente entregou —
        // a diferença vira troco, sem alterar o que é validado contra o total (RegisterPaymentsCommandHandler).
        var payments = new List<PaymentInput> { new("CASH", 198m, 200m, null, null, null, 1, false) };
        var result = await sender.Send(new RegisterPaymentsCommand(sessionId, payments));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Change.Should().Be(2m);
        result.Value.Payments.Single().ChangeAmount.Should().Be(2m);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var financialEntry = await verifyDb.FinancialEntries.SingleAsync(f => f.ReferenceId == sessionId);
        financialEntry.Amount.Should().Be(198m);
    }

    /// <summary>US-058 §4, cenário "Valor líquido calculado": taxa de 2,8% da Cielo sobre R$100 em crédito gera R$97,20 líquidos.</summary>
    [Fact]
    public async Task Pagamento_De_Maquininha_Calcula_Valor_Liquido_Pela_Taxa_Do_Provedor()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m);
        await SeedTenantPaymentProviderFeeAsync(world.TenantId, "CIELO", "CREDIT", 2.8m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Broto", "Única", unitPrice: 100m);
        var (sessionId, _) = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var payments = new List<PaymentInput> { new("CREDIT", 100m, null, "CIELO", "NSU123456", "VISA", 1, false) };
        var result = await sender.Send(new RegisterPaymentsCommand(sessionId, payments));

        result.IsSuccess.Should().BeTrue();
        var registered = result.Value!.Payments.Single();
        registered.FeeAmount.Should().Be(2.80m);
        registered.NetAmount.Should().Be(97.20m);
        registered.ReconciliationStatus.Should().Be("PENDING", "US-058: pagamento com provedor externo aguarda conciliação");
    }

    /// <summary>US-058 §4, cenário "Referência duplicada": mesmo NSU no mesmo tenant exige confirmação explícita antes de aceitar de novo.</summary>
    [Fact]
    public async Task Referencia_Duplicada_Exige_Confirmacao_Explicita()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Familia", "Única", unitPrice: 50m);

        var (firstSessionId, _) = await OpenSessionAsync(world, tableLabel: "10");
        var (secondSessionId, _) = await OpenSessionAsync(world, tableLabel: "11");

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(firstSessionId, produto, 1, null, null, null));
        await sender.Send(new AddOrderItemCommand(secondSessionId, produto, 1, null, null, null));

        var firstPayment = new List<PaymentInput> { new("CREDIT", 50m, null, "CIELO", "NSU-DUP-1", null, 1, false) };
        var first = await sender.Send(new RegisterPaymentsCommand(firstSessionId, firstPayment));
        first.IsSuccess.Should().BeTrue();

        var secondPaymentNoConfirm = new List<PaymentInput> { new("CREDIT", 50m, null, "CIELO", "NSU-DUP-1", null, 1, false) };
        var secondWithoutConfirm = await sender.Send(new RegisterPaymentsCommand(secondSessionId, secondPaymentNoConfirm));
        secondWithoutConfirm.IsSuccess.Should().BeFalse();
        secondWithoutConfirm.Code.Should().Be(ApiErrorCodes.PaymentDuplicateReference);

        var secondPaymentConfirmed = new List<PaymentInput> { new("CREDIT", 50m, null, "CIELO", "NSU-DUP-1", null, 1, true) };
        var secondConfirmed = await sender.Send(new RegisterPaymentsCommand(secondSessionId, secondPaymentConfirmed));
        secondConfirmed.IsSuccess.Should().BeTrue("US-058 §4: reenviar com confirmDuplicate=true aceita o registro mesmo assim");
    }

    /// <summary>US-058 §4: duplicidade de NSU é aviso no mesmo turno/dia; referência histórica não deve bloquear o caixa hoje.</summary>
    [Fact]
    public async Task Referencia_Igual_De_Dia_Anterior_Nao_Exige_Confirmacao()
    {
        var world = await SeedWorldAsync();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Turno Atual", "Única", unitPrice: 50m);
        var (sessionId, _) = await OpenSessionAsync(world);

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var previousPayment = Payment.Create(
                world.TenantId,
                world.StoreId,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                PaymentMethod.Credit,
                amount: 50m,
                netAmount: 50m,
                provider: "CIELO",
                providerRef: "NSU-HISTORICO",
                createdBy: Guid.NewGuid());
            previousPayment.MarkPaid(DateTimeOffset.UtcNow.AddDays(-1));
            seedDb.Payments.Add(previousPayment);
            await seedDb.SaveChangesAsync();
        }

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

        var payment = new List<PaymentInput> { new("CREDIT", 50m, null, "CIELO", "NSU-HISTORICO", null, 1, false) };
        var result = await sender.Send(new RegisterPaymentsCommand(sessionId, payment));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
    }

    /// <summary>US-058 §4: a duplicidade de NSU é sinalizada no mesmo turno, não em todo o histórico do tenant.</summary>
    [Fact]
    public async Task Referencia_Igual_Em_Outro_Turno_Nao_Exige_Confirmacao_De_Duplicidade()
    {
        var world = await SeedWorldAsync();
        var operatorId = Guid.NewGuid();
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m);
        var produto = await SeedProductAsync(world.TenantId, "Pizza Turno", "Única", unitPrice: 50m);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId, operatorId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var firstCashSession = await OpenCashSessionAsync(world, operatorId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), openedAt: DateTimeOffset.UtcNow.AddDays(-1));
        var (firstSessionId, _) = await OpenSessionAsync(world, tableLabel: "21");
        await sender.Send(new AddOrderItemCommand(firstSessionId, produto, 1, null, null, null));

        var firstPayment = new List<PaymentInput> { new("CREDIT", 50m, null, "CIELO", "NSU-OUTRO-TURNO", null, 1, false) };
        var first = await sender.Send(new RegisterPaymentsCommand(firstSessionId, firstPayment));
        first.IsSuccess.Should().BeTrue();

        await CloseCashSessionAsync(world, firstCashSession, operatorId);
        await OpenCashSessionAsync(world, operatorId, DateOnly.FromDateTime(DateTime.UtcNow), openedAt: DateTimeOffset.UtcNow);

        var (secondSessionId, _) = await OpenSessionAsync(world, tableLabel: "22");
        await sender.Send(new AddOrderItemCommand(secondSessionId, produto, 1, null, null, null));

        var secondPayment = new List<PaymentInput> { new("CREDIT", 50m, null, "CIELO", "NSU-OUTRO-TURNO", null, 1, false) };
        var second = await sender.Send(new RegisterPaymentsCommand(secondSessionId, secondPayment));

        second.IsSuccess.Should().BeTrue("US-058 limita o aviso de duplicidade ao mesmo turno de caixa");
    }

    /// <summary>
    /// US-052 §12 ("Propriedade: soma dos pagamentos sempre igual ao total, para qualquer
    /// combinação"). Mesmo estilo de teste de propriedade de <c>BillSplitCalculatorTests</c>
    /// (seed fixa, vários itens/formas aleatórios) — sem FsCheck neste projeto, várias rodadas com
    /// <see cref="Random"/> semeado é a convenção já estabelecida.
    /// </summary>
    [Fact]
    public async Task Soma_Dos_Pagamentos_Sempre_Igual_Ao_Total_Para_Combinacoes_Aleatorias_De_Formas()
    {
        var random = new Random(52052);
        string[] methods = { "CASH", "CREDIT", "DEBIT", "PIX" };

        for (var trial = 0; trial < 12; trial++)
        {
            var world = await SeedWorldAsync();
            await SeedTenantServiceFeeAsync(world.TenantId, percent: 0m);
            var unitPrice = Math.Round((decimal)(random.NextDouble() * 300 + 10), 2, MidpointRounding.AwayFromZero);
            var produto = await SeedProductAsync(world.TenantId, $"Produto {trial}", "Único", unitPrice);
            var (sessionId, _) = await OpenSessionAsync(world, tableLabel: $"P{trial}");

            var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
            await using var db = _fixture.CreateAppDbContext(tenantContext);
            await using var provider = BuildContainer(db, tenantContext);
            var sender = provider.GetRequiredService<ISender>();
            await sender.Send(new AddOrderItemCommand(sessionId, produto, 1, null, null, null));

            // Divide o total em 2-4 partes aleatórias que somam EXATAMENTE o total (mesma técnica
            // de resíduo na primeira parcela de BillSplitCalculator.DistributeEqually).
            var partsCount = random.Next(2, 5);
            var shares = new decimal[partsCount];
            var baseShare = Math.Round(unitPrice / partsCount, 2, MidpointRounding.AwayFromZero);
            for (var i = 0; i < partsCount; i++)
            {
                shares[i] = baseShare;
            }
            shares[0] += unitPrice - (baseShare * partsCount);

            var payments = shares
                .Select(amount => new PaymentInput(methods[random.Next(methods.Length)], amount, null, null, null, null, 1, false))
                .ToList();

            var result = await sender.Send(new RegisterPaymentsCommand(sessionId, payments));

            result.IsSuccess.Should().BeTrue($"trial {trial}: soma de {string.Join(",", shares)} deveria bater com o total {unitPrice}");
            result.Value!.Payments.Sum(p => p.Amount).Should().Be(unitPrice);
        }
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

    private async Task SeedTenantPaymentProviderFeeAsync(Guid tenantId, string provider, string method, decimal percent)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var config = await db.TenantConfigs.SingleOrDefaultAsync(c => c.TenantId == tenantId);
        if (config is null)
        {
            config = TenantConfig.Create(tenantId);
            db.TenantConfigs.Add(config);
        }

        var percentText = percent.ToString(System.Globalization.CultureInfo.InvariantCulture);
        config.UpdatePayments($$"""{ "providers": [ { "code": "{{provider}}", "fees": { "{{method}}": {{percentText}} } } ] }""");
        await db.SaveChangesAsync();
    }

    private async Task<(Guid SessionId, Guid TableId)> OpenSessionAsync(World world, string tableLabel = "1")
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

        return (session.Id, table.Id);
    }

    private async Task<Guid> OpenCashSessionAsync(World world, Guid operatorId, DateOnly businessDay, DateTimeOffset openedAt)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId, operatorId));
        var cashSession = CashSession.Create(world.TenantId, world.StoreId, operatorId, businessDay, openingAmount: 0m, openedAt);
        db.CashSessions.Add(cashSession);
        await db.SaveChangesAsync();
        return cashSession.Id;
    }

    private async Task CloseCashSessionAsync(World world, Guid cashSessionId, Guid operatorId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId, operatorId));
        var cashSession = await db.CashSessions.SingleAsync(s => s.Id == cashSessionId);
        cashSession.SetExpectedAmount(0m);
        cashSession.Close(operatorId, countedAmount: 0m, closedAt: DateTimeOffset.UtcNow);
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
