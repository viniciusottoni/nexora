using Nexora.Application.Installations.Commands.ConsumeInstallationToken;
using Nexora.Application.Installations.Commands.ReissueInstallationToken;
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
/// US-156 · Recuperação do provisionamento e token de instalação — prova os cenários Gherkin
/// ("Resposta de criação foi perdida", "Reemissão segura", "Token já consumido") de ponta a ponta
/// contra Postgres real (<see cref="PostgresFixture"/>, mesma infraestrutura de
/// <c>ConsumeInstallationTokenIntegrationTests</c>/<c>ProvisionTenantIntegrationTests</c>), mais o
/// requisito de concorrência do DoD ("duas reemissões simultâneas deixam só UMA credencial válida")
/// e o requisito de segurança ("segredo nunca em log/evento/histórico").
/// </summary>
[Collection("Postgres")]
public sealed class ReissueInstallationTokenIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public ReissueInstallationTokenIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReissueToken_Emite_Nova_Credencial_Sem_Duplicar_Tenant_Loja_Ou_Instalacao()
    {
        var (tenantId, storeId, installationId) = await SeedPendingInstallationAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ReissueInstallationTokenCommand(
            installationId, "Comando original não foi exibido", 24, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value!.InstallToken.Should().NotBeNullOrWhiteSpace();
        result.Value!.InstallCommand.Should().Contain(tenantId.ToString());

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        (await readDb.Tenants.CountAsync(t => t.Id == tenantId)).Should().Be(1);
        (await readDb.Stores.CountAsync(s => s.Id == storeId)).Should().Be(1);
        (await readDb.EdgeInstallations.CountAsync(e => e.Id == installationId)).Should().Be(1);
        (await readDb.InstallationCredentials.CountAsync(c => c.InstallationId == installationId)).Should().Be(1);
    }

    [Fact]
    public async Task ReissueToken_Revoga_A_Credencial_Pendente_Anterior_E_O_Token_Antigo_Para_De_Funcionar()
    {
        var (tenantId, _, installationId) = await SeedPendingInstallationAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var first = await sender.Send(new ReissueInstallationTokenCommand(
            installationId, "Comando original não foi exibido", 24, Guid.NewGuid()));
        first.IsSuccess.Should().BeTrue();
        var firstRawToken = first.Value!.InstallToken;

        var second = await sender.Send(new ReissueInstallationTokenCommand(
            installationId, "Ainda não copiei o comando", 24, Guid.NewGuid()));
        second.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var credentials = await readDb.InstallationCredentials
            .Where(c => c.InstallationId == installationId)
            .ToListAsync();

        credentials.Should().HaveCount(2);
        credentials.Single(c => c.Id == first.Value!.CredentialId).RevokedAt.Should().NotBeNull();
        credentials.Single(c => c.Id == second.Value!.CredentialId).RevokedAt.Should().BeNull();

        // Gherkin "Reemissão segura": o token da PRIMEIRA reemissão deixou de funcionar
        // imediatamente — consumi-lo agora falha (o hash já não bate com nada em EdgeInstallation).
        await using var consumeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var consumeProvider = MediatRTestContainerFactory.Build(consumeDb, new StaticTenantContext(tenantId));
        var consumeSender = consumeProvider.GetRequiredService<ISender>();

        var consumeOld = await consumeSender.Send(new ConsumeInstallationTokenCommand(firstRawToken));
        consumeOld.IsSuccess.Should().BeFalse();
        consumeOld.Code.Should().Be(ApiErrorCodes.InstallationNotFound);

        var consumeNew = await consumeSender.Send(new ConsumeInstallationTokenCommand(second.Value!.InstallToken));
        consumeNew.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReissueToken_Instalacao_Ja_Registrada_Retorna_Installation_Already_Registered()
    {
        var (tenantId, storeId, _) = await SeedPendingInstallationAsync();

        Guid installationId;
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var installation = await seedDb.EdgeInstallations.SingleAsync(e => e.TenantId == tenantId);
            installation.CompleteRegistration("chave-publica-ed25519", "1.0.0", null);
            await seedDb.SaveChangesAsync();
            installationId = installation.Id;
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ReissueInstallationTokenCommand(
            installationId, "Tentando recuperar após pareamento", 24, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.InstallationAlreadyRegistered);
    }

    [Fact]
    public async Task ReissueToken_Segredo_Bruto_E_Hash_Nunca_Aparecem_No_AuditLog_Nem_No_DomainEvent()
    {
        var (tenantId, _, installationId) = await SeedPendingInstallationAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ReissueInstallationTokenCommand(
            installationId, "Comando original não foi exibido", 24, Guid.NewGuid()));
        result.IsSuccess.Should().BeTrue();

        var rawToken = result.Value!.InstallToken;

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var credential = await readDb.InstallationCredentials.SingleAsync(c => c.InstallationId == installationId);
        var tokenHash = credential.TokenHash;

        var auditLog = await readDb.AuditLogs
            .SingleAsync(a => a.Action == "INSTALLATION_TOKEN_REISSUED" && a.EntityId == installationId);
        var domainEvent = await readDb.DomainEvents
            .SingleAsync(e => e.Type == "installation.token_reissued" && e.AggregateId == installationId);

        auditLog.After.Should().NotBeNullOrEmpty();
        auditLog.After.Should().NotContain(rawToken);
        auditLog.After.Should().NotContain(tokenHash);

        domainEvent.Payload.Should().NotContain(rawToken);
        domainEvent.Payload.Should().NotContain(tokenHash);
    }

    [Fact]
    public async Task ReissueToken_Duas_Reemissoes_Simultaneas_Deixam_Apenas_Uma_Credencial_Ativa()
    {
        var (tenantId, _, installationId) = await SeedPendingInstallationAsync();

        await using var dbA = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var providerA = MediatRTestContainerFactory.Build(dbA, new StaticTenantContext(tenantId));
        var senderA = providerA.GetRequiredService<ISender>();

        await using var dbB = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var providerB = MediatRTestContainerFactory.Build(dbB, new StaticTenantContext(tenantId));
        var senderB = providerB.GetRequiredService<ISender>();

        var taskA = senderA.Send(new ReissueInstallationTokenCommand(installationId, "Requisição A", 24, Guid.NewGuid()));
        var taskB = senderB.Send(new ReissueInstallationTokenCommand(installationId, "Requisição B", 24, Guid.NewGuid()));

        await Task.WhenAll(taskA, taskB);

        // SELECT ... FOR UPDATE (LockEdgeInstallationForUpdateAsync) serializa as duas transações —
        // nenhuma delas falha (não é uma corrida de "quem chega primeiro vence, o outro recebe
        // erro"), as duas emitem uma credencial, mas a segunda a COMMITAR revoga a que a primeira
        // acabou de criar antes de emitir a sua própria.
        taskA.Result.IsSuccess.Should().BeTrue();
        taskB.Result.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var credentials = await readDb.InstallationCredentials
            .Where(c => c.InstallationId == installationId)
            .ToListAsync();

        credentials.Should().HaveCount(2);
        credentials.Count(c => c.RevokedAt is null && c.ConsumedAt is null).Should().Be(1);
    }

    private async Task<(Guid TenantId, Guid StoreId, Guid InstallationId)> SeedPendingInstallationAsync()
    {
        var tenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        Guid storeId;
        Guid installationId;
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var store = Store.Create(tenantId, "Matriz", isDefault: true, timezone: "America/Sao_Paulo");
            db.Stores.Add(store);

            var installation = EdgeInstallation.Create(tenantId, store.Id, "Servidor local — Matriz");
            var tokenDigester = new Nexora.Infrastructure.Installations.InstallationTokenDigester();
            installation.IssueInstallToken(tokenDigester.Digest("raw-install-token-original"), DateTimeOffset.UtcNow.AddHours(24));
            db.EdgeInstallations.Add(installation);

            await db.SaveChangesAsync();
            storeId = store.Id;
            installationId = installation.Id;
        }

        return (tenantId, storeId, installationId);
    }
}
