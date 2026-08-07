using Nexora.Application.Platform.Commands.AcknowledgeAttentionItem;
using Nexora.Application.Platform.Queries.GetAttentionQueue;
using Nexora.Application.Tenants.Commands.ProvisionTenant;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — prova os cenários Gherkin
/// "Priorização explicável" e "Atalho de suporte" (implícito: nenhum token é criado por
/// <c>AcknowledgeAttentionItemCommand</c>) de ponta a ponta, pelo pipeline MediatR real, contra
/// Postgres real com RLS ligado (mesma infraestrutura de <see cref="TenantPlanIntegrationTests"/>).
/// Cobre especificamente "múltiplas fontes agregadas corretamente, sem perda nem duplicidade" —
/// os três tipos de pendência (instalação offline, convite expirado, provisionamento parado) vêm de
/// TRÊS tabelas diferentes e devem aparecer juntos, uma vez cada, na mesma fila.
/// </summary>
[Collection("Postgres")]
public sealed class AdministrativeAttentionQueueIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public AdministrativeAttentionQueueIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário: "Priorização explicável" — instalação offline, convite expirado e provisionamento parado aparecem juntos, cada um com severidade/motivo/tempo na condição, ordenados por criticidade sem esconder os menos graves.</summary>
    [Fact]
    public async Task GetAttentionQueue_Agrega_As_Tres_Fontes_Sem_Perda_Nem_Duplicidade_E_Ordena_Por_Criticidade()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(BuildProvisionCommand(slug));
        provisioned.IsSuccess.Should().BeTrue();
        var tenantId = provisioned.Value!.Tenant.Id;
        var storeId = provisioned.Value.Store.Id;
        var ownerInviteId = await GetOwnerInviteIdAsync(tenantId);

        // Arranjo: tenant "parado" em PROVISIONED há mais de 4 h (limiar mínimo — ver
        // AttentionQueueClassifier.ProvisioningStalledMinimumThreshold), convite já expirado, e uma
        // instalação edge instalada mas sem contato há mais de 15 min (DOWN, ver
        // InstallationHealthClassifier.DownThreshold).
        await BackdateTenantCreatedAtAsync(tenantId, TimeSpan.FromHours(30));
        await BackdateOwnerInviteExpiresAtAsync(tenantId, ownerInviteId, TimeSpan.FromDays(1));
        var installationId = await CreateOfflineInstallationAsync(tenantId, storeId, TimeSpan.FromMinutes(20));

        await using var queryDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var queryProvider = MediatRTestContainerFactory.Build(queryDb, new StaticTenantContext(tenantId: null));
        var querySender = queryProvider.GetRequiredService<ISender>();

        var result = await querySender.Send(new GetAttentionQueueQuery(Array.Empty<Nexora.Application.Platform.Support.AttentionSeverity>(), 25, null));

        result.IsSuccess.Should().BeTrue();
        var items = result.Value!.Data.Where(i => i.TenantId == tenantId).ToList();

        items.Should().HaveCount(3, "cada uma das três fontes deve contribuir exatamente um item, sem perda nem duplicidade");
        items.Select(i => i.Type).Should().BeEquivalentTo(new[] { "INSTALLATION_OFFLINE", "INVITE_EXPIRED", "PROVISIONING_STALLED" });

        var installationItem = items.Single(i => i.Type == "INSTALLATION_OFFLINE");
        installationItem.Severity.Should().Be("HIGH");
        installationItem.Reason.Should().Contain("Sem contato há");
        installationItem.Action.Kind.Should().Be("OPEN_DIAGNOSTICS");

        var inviteItem = items.Single(i => i.Type == "INVITE_EXPIRED");
        inviteItem.Severity.Should().Be("MEDIUM");
        inviteItem.Reason.Should().Contain("Convite expirado há");
        inviteItem.Action.Kind.Should().Be("OPEN_TENANT");
        inviteItem.Action.Href.Should().Be($"/estabelecimentos/{tenantId}");

        var provisioningItem = items.Single(i => i.Type == "PROVISIONING_STALLED");
        provisioningItem.Severity.Should().Be("HIGH");
        provisioningItem.Reason.Should().Contain("Provisionamento parado em PROVISIONED");

        // Ordenação por criticidade: os três itens deste tenant não são todos CRITICAL, então a
        // fila inteira (que pode incluir outros tenants de outros testes) precisa mostrar TODOS os
        // três — "sem esconder itens menos graves".
        var ranks = new Dictionary<string, int> { ["CRITICAL"] = 0, ["HIGH"] = 1, ["MEDIUM"] = 2, ["LOW"] = 3 };
        result.Value.Data.Select(i => ranks[i.Severity]).Should().BeInAscendingOrder();

        result.Value.Meta.UnavailableSources.Should().BeEmpty("nenhuma fonte falhou neste cenário");

        // guarda o installationId só para não gerar warning de variável não usada em builds futuros
        installationId.Should().NotBeEmpty();
    }

    /// <summary>Cenário: "Falha parcial" complementar ao Gherkin — filtro por severidade restringe a fila sem afetar outras fontes.</summary>
    [Fact]
    public async Task GetAttentionQueue_Filtra_Por_Severidade()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(BuildProvisionCommand(slug));
        var tenantId = provisioned.Value!.Tenant.Id;
        var storeId = provisioned.Value.Store.Id;

        // Instalação offline há muito tempo -> CRITICAL (>= InstallationOfflineCriticalThreshold).
        await CreateOfflineInstallationAsync(tenantId, storeId, TimeSpan.FromHours(2));

        await using var queryDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var queryProvider = MediatRTestContainerFactory.Build(queryDb, new StaticTenantContext(tenantId: null));
        var querySender = queryProvider.GetRequiredService<ISender>();

        var onlyCritical = await querySender.Send(new GetAttentionQueueQuery(
            new[] { Nexora.Application.Platform.Support.AttentionSeverity.Critical }, 25, null));

        onlyCritical.IsSuccess.Should().BeTrue();
        onlyCritical.Value!.Data.Where(i => i.TenantId == tenantId).Should().ContainSingle()
            .Which.Severity.Should().Be("CRITICAL");

        var onlyLow = await querySender.Send(new GetAttentionQueueQuery(
            new[] { Nexora.Application.Platform.Support.AttentionSeverity.Low }, 25, null));

        onlyLow.IsSuccess.Should().BeTrue();
        onlyLow.Value!.Data.Should().NotContain(i => i.TenantId == tenantId);
    }

    /// <summary>
    /// Cenário: "Reconhecimento/resolução de pendência administrativa SEM apagar o fato original" —
    /// reconhecer some da fila ativa, mas a fonte original (tenant ainda PROVISIONED) continua intacta.
    /// </summary>
    [Fact]
    public async Task AcknowledgeAttentionItem_Suprime_O_Item_Sem_Apagar_O_Fato_Original()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(BuildProvisionCommand(slug));
        var tenantId = provisioned.Value!.Tenant.Id;
        await BackdateTenantCreatedAtAsync(tenantId, TimeSpan.FromHours(30));

        await using var queryDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var queryProvider = MediatRTestContainerFactory.Build(queryDb, new StaticTenantContext(tenantId: null));
        var querySender = queryProvider.GetRequiredService<ISender>();

        var before = await querySender.Send(new GetAttentionQueueQuery(Array.Empty<Nexora.Application.Platform.Support.AttentionSeverity>(), 25, null));
        var itemId = before.Value!.Data.Single(i => i.TenantId == tenantId && i.Type == "PROVISIONING_STALLED").Id;

        var actorId = Guid.NewGuid();
        var ack = await querySender.Send(new AcknowledgeAttentionItemCommand(itemId, "Cliente avisado, aguardando retorno.", actorId));
        ack.IsSuccess.Should().BeTrue();
        ack.Value!.ItemId.Should().Be(itemId);

        var after = await querySender.Send(new GetAttentionQueueQuery(Array.Empty<Nexora.Application.Platform.Support.AttentionSeverity>(), 25, null));
        after.Value!.Data.Should().NotContain(i => i.Id == itemId, "o item reconhecido não deve mais aparecer na fila ativa");

        // O fato original (tenant ainda PROVISIONED) não foi alterado — RN-004.
        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId);
        tenant.Status.Should().Be(Nexora.Domain.Platform.TenantStatus.Provisioned);

        var ackRow = await readDb.AdministrativeAttentionAcknowledgements.AsNoTracking().SingleAsync(a => a.ItemId == itemId);
        ackRow.Reason.Should().Be("Cliente avisado, aguardando retorno.");
        ackRow.ActorId.Should().Be(actorId);
    }

    [Fact]
    public async Task AcknowledgeAttentionItem_Com_Chave_Malformada_Retorna_404()
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new AcknowledgeAttentionItemCommand("chave-invalida", "Motivo qualquer", Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(Nexora.Shared.Errors.ApiErrorCodes.AttentionItemNotFound);
    }

    [Fact]
    public async Task AcknowledgeAttentionItem_Com_Chave_Bem_Formada_Mas_Sem_Pendencia_Ativa_Retorna_404()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(BuildProvisionCommand(slug));
        var tenantId = provisioned.Value!.Tenant.Id;
        var fabricatedItemId = Nexora.Application.Platform.Support.AttentionItemId.Encode(
            Nexora.Application.Platform.Support.AttentionItemType.InviteExpired,
            tenantId,
            Guid.NewGuid());

        var result = await sender.Send(new AcknowledgeAttentionItemCommand(
            fabricatedItemId,
            "Tentativa de reconhecer item inexistente.",
            Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(Nexora.Shared.Errors.ApiErrorCodes.AttentionItemNotFound);
    }

    /// <summary>
    /// Gherkin "Atalho de suporte": <see cref="AcknowledgeAttentionItemCommand"/> é o único comando
    /// de escrita desta US — prova, contra o banco real, que ele nunca cria uma linha em
    /// <c>support_access</c> (RN-015: nenhum atalho contorna a autorização da US-145).
    /// </summary>
    [Fact]
    public async Task AcknowledgeAttentionItem_Nunca_Cria_Registro_De_Acesso_De_Suporte()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(BuildProvisionCommand(slug));
        var tenantId = provisioned.Value!.Tenant.Id;
        await BackdateTenantCreatedAtAsync(tenantId, TimeSpan.FromHours(30));

        await using var queryDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var queryProvider = MediatRTestContainerFactory.Build(queryDb, new StaticTenantContext(tenantId: null));
        var querySender = queryProvider.GetRequiredService<ISender>();

        var queue = await querySender.Send(new GetAttentionQueueQuery(Array.Empty<Nexora.Application.Platform.Support.AttentionSeverity>(), 25, null));
        var itemId = queue.Value!.Data.Single(i => i.TenantId == tenantId).Id;

        var ack = await querySender.Send(new AcknowledgeAttentionItemCommand(itemId, "Sem ação necessária no momento.", Guid.NewGuid()));
        ack.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        (await readDb.SupportAccesses.AsNoTracking().CountAsync(s => s.TenantId == tenantId)).Should().Be(0);
    }

    private static ProvisionTenantCommand BuildProvisionCommand(string slug) => new(
        Name: "Pizzaria Dona Betinha",
        Slug: slug,
        Plan: "STANDARD",
        Template: "PIZZERIA",
        OwnerName: "Dona Betinha",
        OwnerEmail: $"owner-{Guid.NewGuid():N}@example.com",
        StoreName: "Matriz",
        StoreTimezone: "America/Sao_Paulo");

    private static string UniqueSlug() => $"tenant-attention-{Guid.NewGuid():N}";

    private async Task<Guid> GetOwnerInviteIdAsync(Guid tenantId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        return (await db.OwnerInvites.AsNoTracking().SingleAsync(i => i.TenantId == tenantId)).Id;
    }

    private async Task BackdateTenantCreatedAtAsync(Guid tenantId, TimeSpan age)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE tenant SET created_at = now() - @age WHERE id = @id";
        cmd.Parameters.AddWithValue("age", age);
        cmd.Parameters.AddWithValue("id", tenantId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task BackdateOwnerInviteExpiresAtAsync(Guid tenantId, Guid inviteId, TimeSpan pastExpiry)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var setTenant = connection.CreateCommand();
        setTenant.CommandText = $"SET app.tenant_id = '{tenantId}'";
        await setTenant.ExecuteNonQueryAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE owner_invite SET expires_at = now() - @age WHERE id = @id";
        cmd.Parameters.AddWithValue("age", pastExpiry);
        cmd.Parameters.AddWithValue("id", inviteId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Marca como pareada, com <c>last_seen_at</c> retroativo o suficiente para ser classificada
    /// DOWN, a instalação edge que <c>ProvisionTenantCommandHandler</c> já cria (não pareada) para
    /// toda loja — <c>uq_edge_store</c> permite só UMA instalação por loja, então o arranjo de teste
    /// reaproveita a existente em vez de criar outra.
    /// </summary>
    private async Task<Guid> CreateOfflineInstallationAsync(Guid tenantId, Guid storeId, TimeSpan staleness)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var installation = await db.EdgeInstallations.SingleAsync(i => i.TenantId == tenantId && i.StoreId == storeId);
        installation.MarkInstalled("chave-publica-de-teste");
        await db.SaveChangesAsync(CancellationToken.None);

        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var setTenant = connection.CreateCommand();
        setTenant.CommandText = $"SET app.tenant_id = '{tenantId}'";
        await setTenant.ExecuteNonQueryAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE edge_installation SET last_seen_at = now() - @staleness WHERE id = @id";
        cmd.Parameters.AddWithValue("staleness", staleness);
        cmd.Parameters.AddWithValue("id", installation.Id);
        await cmd.ExecuteNonQueryAsync();

        return installation.Id;
    }
}
