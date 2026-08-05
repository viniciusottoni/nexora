using System.Globalization;
using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Operation.Abstractions;
using Nexora.Application.Tables.Commands.CreateTablesBulk;
using Nexora.Application.Tables.Commands.DeleteTable;
using Nexora.Application.Tables.Commands.RotateTableQrToken;
using Nexora.Application.Tables.Commands.SetTableActive;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Operation;
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
/// Cenários Gherkin da US-020 contra um PostgreSQL real (Testcontainers), mesmo pipeline MediatR
/// de produção (Validation -&gt; Logging -&gt; Transaction, ver <see cref="BuildMediatRContainer"/>):
/// "Criação em lote" (transacional), "Rotação de token" (invalidação imediata do token anterior) e
/// "Exclusão de mesa com histórico" (recusada, desativação como alternativa).
/// </summary>
[Collection("Postgres")]
public sealed class OperationTablesIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public OperationTablesIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Criação em lote": 20 mesas com rótulos sequenciais e qr_token único e opaco cada.</summary>
    [Fact]
    public async Task Criacao_Em_Lote_Cria_Mesas_Sequenciais_Com_Token_Unico_Cada()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildMediatRContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateTablesBulkCommand(areaId, 1, 20, 4));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(20);

        var tables = await db.DiningTables.Where(t => t.AreaId == areaId).ToListAsync();
        tables.Should().HaveCount(20);
        tables.Select(t => t.Label).Should().BeEquivalentTo(Enumerable.Range(1, 20).Select(n => n.ToString(CultureInfo.InvariantCulture)));
        tables.Select(t => t.QrToken).Distinct().Should().HaveCount(20, "cada mesa precisa de um qr_token único e opaco");
    }

    /// <summary>
    /// Cenário Gherkin "Criação em lote" (aspecto transacional): se um rótulo do intervalo já
    /// existir, NENHUMA mesa do lote deve ser gravada — tudo ou nada, nunca um lote parcial.
    /// </summary>
    [Fact]
    public async Task Criacao_Em_Lote_Com_Rotulo_Conflitante_Nao_Grava_Nenhuma_Mesa_Do_Lote()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            seedDb.DiningTables.Add(DiningTable.Create(tenantId, storeId, areaId, "10", "token-preexistente", 4));
            await seedDb.SaveChangesAsync();
        }

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildMediatRContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateTablesBulkCommand(areaId, 1, 20, 4));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.TableLabelAlreadyExists);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var tableCount = await verifyDb.DiningTables.CountAsync(t => t.AreaId == areaId);
        tableCount.Should().Be(1, "a mesa '10' pré-existente é a única — nenhuma mesa do lote de 1 a 20 deveria ter sido gravada");
    }

    /// <summary>Cenário Gherkin "Rotação de token": o código anterior deixa de funcionar imediatamente.</summary>
    [Fact]
    public async Task Rotacionar_Token_Invalida_O_Token_Anterior_Imediatamente()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);

        Guid tableId;
        string originalToken;
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            var seedTable = DiningTable.Create(tenantId, storeId, areaId, "5", "token-original-fotografado", 4);
            seedDb.DiningTables.Add(seedTable);
            await seedDb.SaveChangesAsync();
            tableId = seedTable.Id;
            originalToken = seedTable.QrToken;
        }

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildMediatRContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RotateTableQrTokenCommand(tableId));

        result.IsSuccess.Should().BeTrue();

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var table = await verifyDb.DiningTables.SingleAsync(t => t.Id == tableId);
        table.QrToken.Should().NotBe(originalToken);

        var resolvedByOldToken = await verifyDb.DiningTables.AnyAsync(t => t.QrToken == originalToken);
        resolvedByOldToken.Should().BeFalse("o token anterior deve deixar de resolver imediatamente, sem prazo de carência");
    }

    /// <summary>
    /// Cenário Gherkin "Exclusão de mesa com histórico": mesa com sessão encerrada não pode ser
    /// excluída — a desativação é a alternativa oferecida e continua funcionando normalmente.
    /// </summary>
    [Fact]
    public async Task Exclusao_De_Mesa_Com_Historico_E_Recusada_Mas_Desativacao_Funciona()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);

        Guid tableId;
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            var table = DiningTable.Create(tenantId, storeId, areaId, "7", "token-mesa-7", 4);
            seedDb.DiningTables.Add(table);
            tableId = table.Id;

            var session = TableSession.Create(tenantId, storeId, tableId, DateOnly.FromDateTime(DateTime.UtcNow));
            session.RequestBill();
            session.MarkAsPaid(50m, 0m, 0m, 50m);
            session.Close();
            seedDb.TableSessions.Add(session);

            await seedDb.SaveChangesAsync();
        }

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildMediatRContainer(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var deleteResult = await sender.Send(new DeleteTableCommand(tableId));
        deleteResult.IsSuccess.Should().BeFalse();
        deleteResult.Code.Should().Be(ApiErrorCodes.TableHasSessionHistory);

        await using var afterDeleteDb = _fixture.CreateAppDbContext(tenantContext);
        var stillThere = await afterDeleteDb.DiningTables.SingleAsync(t => t.Id == tableId);
        stillThere.DeletedAt.Should().BeNull("a exclusão foi recusada — a mesa não pode ter sido excluída");

        await using var deactivateDb = _fixture.CreateAppDbContext(tenantContext);
        await using var deactivateProvider = BuildMediatRContainer(deactivateDb, tenantContext);
        var deactivateResult = await deactivateProvider.GetRequiredService<ISender>().Send(new SetTableActiveCommand(tableId, false));
        deactivateResult.IsSuccess.Should().BeTrue("a desativação é a alternativa oferecida quando a exclusão é recusada");

        await using var finalDb = _fixture.CreateAppDbContext(tenantContext);
        var deactivated = await finalDb.DiningTables.SingleAsync(t => t.Id == tableId);
        deactivated.IsActive.Should().BeFalse();
        deactivated.DeletedAt.Should().BeNull();
    }

    /// <summary>
    /// Cenário de segurança (US-020 §12, RN-015): o qr_token de uma mesa de um tenant não resolve
    /// sob o contexto de outro tenant — o RLS (ADR-004) nega a linha por completo, sem depender de
    /// nenhum WHERE de aplicação.
    /// </summary>
    [Fact]
    public async Task Qr_Token_De_Uma_Mesa_De_Um_Tenant_Nao_Resolve_Sob_Outro_Tenant()
    {
        var (tenantA, storeA) = await SeedTenantAndStoreAsync();
        var (tenantB, storeB) = await SeedTenantAndStoreAsync();
        var areaA = await SeedAreaAsync(tenantA, storeA);

        string tokenA;
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA, storeA)))
        {
            var table = DiningTable.Create(tenantA, storeA, areaA, "1", "token-tenant-a-nao-adivinhavel", 4);
            seedDb.DiningTables.Add(table);
            await seedDb.SaveChangesAsync();
            tokenA = table.QrToken;
        }

        // Mesmo processo, mesmo código — só troca o tenant do contexto, exatamente como duas
        // requisições HTTP autenticadas para tenants diferentes fariam.
        await using var dbAsTenantB = _fixture.CreateAppDbContext(new StaticTenantContext(tenantB, storeB));

        var resolved = await dbAsTenantB.DiningTables.SingleOrDefaultAsync(t => t.QrToken == tokenA);

        resolved.Should().BeNull("o qr_token da mesa do tenant A não deve resolver nenhuma linha sob o contexto do tenant B");
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

    private static ServiceProvider BuildMediatRContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);

        services.AddSingleton<IQrTokenGenerator, QrTokenGenerator>();

        services.AddValidatorsFromAssembly(typeof(ICommand).Assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services.BuildServiceProvider();
    }
}
