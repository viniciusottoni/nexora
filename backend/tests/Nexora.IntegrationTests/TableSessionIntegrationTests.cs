using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Commands.AccessTableByQrToken;
using Nexora.Application.Tables.Commands.OpenTableSession;
using Nexora.Application.Tables.Commands.UpdateTableSession;
using Nexora.Application.Tables.Queries.GetTableSession;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin das US-022 ("Abertura de mesa") e US-021 ("Acesso ao cardápio pelo QR Code")
/// contra um PostgreSQL real (Testcontainers) — mesmo pipeline MediatR de produção
/// (<see cref="MediatRTestContainerFactory"/>). Prova, em particular, que os dois fluxos de
/// abertura (garçom via <see cref="OpenTableSessionCommand"/> e cliente via
/// <see cref="AccessTableByQrTokenCommand"/>) levam ao MESMO estado: <c>table_session</c> aberta e
/// <c>dining_table</c> ocupada, com <c>EVT-020 table.session.opened</c> gravado na mesma transação.
/// </summary>
[Collection("Postgres")]
public sealed class TableSessionIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public TableSessionIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Abertura pelo garçom" (US-022 §4).</summary>
    [Fact]
    public async Task Abertura_Pelo_Garcom_Cria_Sessao_Open_Origem_Waiter_E_Ocupa_A_Mesa()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "12", "token-mesa-12");
        var waiterId = Guid.NewGuid();

        var tenantContext = new StaticTenantContext(tenantId, storeId, waiterId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new OpenTableSessionCommand(tableId, GuestCount: 4, OccurredAt: null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("OPEN");
        result.Value.Source.Should().Be("WAITER");
        result.Value.GuestCount.Should().Be(4);
        result.Value.GuestCountConfirmed.Should().BeTrue("o garçom sempre informa e confirma a contagem na abertura");
        result.Value.WaiterId.Should().Be(waiterId, "o garçom autenticado é o responsável inicial pela mesa");
        result.Value.CurrentItems.Should().BeEmpty("lançamento de item é escopo de US-030/US-028");
        result.Value.Total.Should().Be(0m);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var table = await verifyDb.DiningTables.SingleAsync(t => t.Id == tableId);
        table.Status.Should().Be(TableStatus.Occupied, "US-022 §4: \"a mesa deve aparecer como ocupada no mapa\"");

        var evt = await verifyDb.DomainEvents.SingleOrDefaultAsync(e => e.AggregateId == result.Value.Id);
        evt.Should().NotBeNull("EVT-020 table.session.opened precisa ser gravado na mesma transação do estado");
        evt!.Type.Should().Be("table.session.opened");
    }

    /// <summary>Cenário Gherkin "Mesa já ocupada" (US-022 §4): 409 direcionado à sessão existente.</summary>
    [Fact]
    public async Task Abrir_Mesa_Ja_Ocupada_Recusa_E_Aponta_Para_A_Sessao_Existente()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "13", "token-mesa-13");

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var firstOpen = await sender.Send(new OpenTableSessionCommand(tableId, GuestCount: 2, OccurredAt: null));
        firstOpen.IsSuccess.Should().BeTrue();

        var secondOpen = await sender.Send(new OpenTableSessionCommand(tableId, GuestCount: 3, OccurredAt: null));

        secondOpen.IsSuccess.Should().BeFalse();
        secondOpen.Code.Should().Be(ApiErrorCodes.TableAlreadyOpen);
        secondOpen.Errors.Should().NotBeNull();
        secondOpen.Errors!["sessionId"].Should().Contain(firstOpen.Value!.Id.ToString());

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var sessionCount = await verifyDb.TableSessions.CountAsync(s => s.TableId == tableId);
        sessionCount.Should().Be(1, "nenhuma segunda sessão deveria ter sido gravada");
    }

    /// <summary>RN-020 (US-022 §9): o X-Occurred-At sobrevive mesmo com sincronização atrasada.</summary>
    [Fact]
    public async Task OccurredAt_Informado_E_Preservado_Mesmo_Que_Diferente_Do_Relogio_Do_Servidor()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "14", "token-mesa-14");
        var occurredAt = DateTimeOffset.UtcNow.AddHours(-5); // "sync atrasado" simulado

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new OpenTableSessionCommand(tableId, GuestCount: 2, OccurredAt: occurredAt));

        result.IsSuccess.Should().BeTrue();
        // BeCloseTo, não Be: timestamptz do Postgres tem precisão de microssegundos (6 dígitos),
        // DateTimeOffset tem precisão de tick (7 dígitos) — o round-trip trunca o último dígito,
        // sem relação nenhuma com a garantia de negócio (RN-020) que este teste prova.
        result.Value!.OpenedAt.Should().BeCloseTo(occurredAt, TimeSpan.FromMilliseconds(1));

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var session = await verifyDb.TableSessions.SingleAsync(s => s.Id == result.Value.Id);
        session.OpenedAt.Should().BeCloseTo(occurredAt, TimeSpan.FromMilliseconds(1));
    }

    /// <summary>
    /// US-022 §9: "abertura de mesa é operação crítica de tempo real e nunca pode depender da
    /// nuvem" — o container MediatR usado aqui não registra NENHUM HttpClient/dependência de rede
    /// para a nuvem (só Postgres local via <see cref="PostgresFixture"/>), e a operação completa
    /// com sucesso mesmo assim — prova, por construção, que o fluxo não faz nenhuma chamada
    /// externa.
    /// </summary>
    [Fact]
    public async Task Abertura_Funciona_Sem_Nenhuma_Dependencia_De_Rede_Externa()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "15", "token-mesa-15");

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);

        // Nenhum HttpClient/ISyncHealthPoller/etc. registrado neste container — se o handler
        // dependesse de qualquer chamada à nuvem, o DI já falharia ao resolver o ISender.
        provider.GetService<HttpClient>().Should().BeNull();

        var result = await provider.GetRequiredService<ISender>()
            .Send(new OpenTableSessionCommand(tableId, GuestCount: 2, OccurredAt: null));

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>Cenário Gherkin "Primeira leitura da mesa" (US-021 §4): abre com origem QR e contagem pendente.</summary>
    [Fact]
    public async Task Primeira_Leitura_Do_Qr_Abre_Sessao_Com_Origem_Qr_E_Contagem_Pendente()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "16", "token-mesa-16-para-qr");

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new AccessTableByQrTokenCommand("token-mesa-16-para-qr"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Session.Source.Should().Be("QR");
        result.Value.Session.GuestCountConfirmed.Should().BeFalse("US-021: contagem pendente de confirmação pelo garçom");
        result.Value.Table.Label.Should().Be("16");
        result.Value.SessionToken.Should().NotBeNullOrWhiteSpace();

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var table = await verifyDb.DiningTables.SingleAsync(t => t.Id == tableId);
        table.Status.Should().Be(TableStatus.Occupied);

        var evt = await verifyDb.DomainEvents.SingleOrDefaultAsync(e => e.AggregateId == result.Value.Session.Id);
        evt.Should().NotBeNull();
        evt!.Type.Should().Be("table.session.opened");
    }

    /// <summary>Cenário Gherkin "Sessão de mesa já aberta" (US-021 §4): entra na sessão existente, sem duplicar.</summary>
    [Fact]
    public async Task Ler_Qr_De_Mesa_Ja_Aberta_Pelo_Garcom_Entra_Na_Sessao_Existente()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "17", "token-mesa-17");

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var openedByWaiter = await sender.Send(new OpenTableSessionCommand(tableId, GuestCount: 5, OccurredAt: null));
        openedByWaiter.IsSuccess.Should().BeTrue();

        var accessedByQr = await sender.Send(new AccessTableByQrTokenCommand("token-mesa-17"));

        accessedByQr.IsSuccess.Should().BeTrue();
        accessedByQr.Value!.Session.Id.Should().Be(openedByWaiter.Value!.Id, "deve entrar na MESMA sessão, não abrir uma segunda");
        accessedByQr.Value.Session.Source.Should().Be("WAITER");
        accessedByQr.Value.Session.GuestCount.Should().Be(5);
        accessedByQr.Value.Session.CurrentItems.Should().BeEmpty("já lançados na mesa — vazio nesta wave (US-030/US-028)");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var sessionCount = await verifyDb.TableSessions.CountAsync(s => s.TableId == tableId);
        sessionCount.Should().Be(1);
    }

    /// <summary>Cenário Gherkin "Token inválido ou rotacionado" (US-021 §4): nenhuma informação de outra mesa vaza.</summary>
    [Fact]
    public async Task Token_Inexistente_Retorna_Invalid_Table_Token_Sem_Revelar_Detalhes()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>()
            .Send(new AccessTableByQrTokenCommand("token-que-nunca-existiu"));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.InvalidTableToken);
    }

    /// <summary>Cenário Gherkin "Token inválido ou rotacionado" (US-021 §4): o token antigo some assim que rotacionado.</summary>
    [Fact]
    public async Task Token_Rotacionado_Deixa_De_Resolver_Imediatamente()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "18", "token-antigo-mesa-18");

        await using (var rotateDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            var table = await rotateDb.DiningTables.SingleAsync(t => t.Id == tableId);
            table.RotateQrToken("token-novo-mesa-18");
            await rotateDb.SaveChangesAsync();
        }

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var withOldToken = await sender.Send(new AccessTableByQrTokenCommand("token-antigo-mesa-18"));
        withOldToken.IsSuccess.Should().BeFalse();
        withOldToken.Code.Should().Be(ApiErrorCodes.InvalidTableToken);

        var withNewToken = await sender.Send(new AccessTableByQrTokenCommand("token-novo-mesa-18"));
        withNewToken.IsSuccess.Should().BeTrue();
    }

    /// <summary>Cenário Gherkin "Troca de garçom responsável" (US-022 §4): ambos ficam registrados no histórico (AuditLog).</summary>
    [Fact]
    public async Task Trocar_O_Garcom_Responsavel_Grava_O_Antigo_E_O_Novo_No_Historico()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "19", "token-mesa-19");
        var joao = Guid.NewGuid();
        var ana = Guid.NewGuid();

        var tenantContext = new StaticTenantContext(tenantId, storeId, joao);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var opened = await sender.Send(new OpenTableSessionCommand(tableId, GuestCount: 2, OccurredAt: null));
        opened.IsSuccess.Should().BeTrue();
        opened.Value!.WaiterId.Should().Be(joao);

        var updated = await sender.Send(new UpdateTableSessionCommand(opened.Value.Id, GuestCount: null, WaiterId: ana));

        updated.IsSuccess.Should().BeTrue();
        updated.Value!.WaiterId.Should().Be(ana);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.EntityId == opened.Value.Id && a.Action == "TABLE_SESSION_WAITER_REASSIGNED");
        audit.Before.Should().Contain(joao.ToString());
        audit.After.Should().Contain(ana.ToString());
    }

    /// <summary>US-021: o garçom confirma a contagem pendente de uma sessão aberta por QR via PATCH.</summary>
    [Fact]
    public async Task Confirmar_Contagem_De_Sessao_Aberta_Por_Qr_Marca_Como_Confirmada()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "20", "token-mesa-20");

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var accessed = await sender.Send(new AccessTableByQrTokenCommand("token-mesa-20"));
        accessed.Value!.Session.GuestCountConfirmed.Should().BeFalse();

        var updated = await sender.Send(new UpdateTableSessionCommand(accessed.Value.Session.Id, GuestCount: 3, WaiterId: null));

        updated.IsSuccess.Should().BeTrue();
        updated.Value!.GuestCount.Should().Be(3);
        updated.Value.GuestCountConfirmed.Should().BeTrue();
    }

    /// <summary>GET /v1/sessions/{id} — consulta simples, já com o contrato de currentItems/total presente.</summary>
    [Fact]
    public async Task Consultar_Sessao_Devolve_Currentitems_Vazio_E_Total_Zero()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var areaId = await SeedAreaAsync(tenantId, storeId);
        var tableId = await SeedTableAsync(tenantId, storeId, areaId, "21", "token-mesa-21");

        var tenantContext = new StaticTenantContext(tenantId, storeId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var opened = await sender.Send(new OpenTableSessionCommand(tableId, GuestCount: 2, OccurredAt: null));

        var fetched = await sender.Send(new GetTableSessionQuery(opened.Value!.Id));

        fetched.IsSuccess.Should().BeTrue();
        fetched.Value!.CurrentItems.Should().BeEmpty();
        fetched.Value.Total.Should().Be(0m);
        fetched.Value.TableLabel.Should().Be("21");
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
