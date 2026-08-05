using Nexora.Application.Installations.Commands.ConsumeInstallationToken;
using Nexora.Application.Installations.Commands.ReissueInstallationToken;
using Nexora.Application.Installations.Commands.RevokeInstallationCredential;
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

/// <summary>US-156 · Recuperação do provisionamento e token de instalação — "revogação manual de token comprometido".</summary>
[Collection("Postgres")]
public sealed class RevokeInstallationCredentialIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public RevokeInstallationCredentialIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RevokeInstallationCredential_Revoga_E_Invalida_O_Token_Corrente()
    {
        var (tenantId, installationId, credentialId, rawToken) = await SeedReissuedCredentialAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RevokeInstallationCredentialCommand(
            installationId, credentialId, "Credencial possivelmente exposta", Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var credential = await readDb.InstallationCredentials.SingleAsync(c => c.Id == credentialId);
        credential.RevokedAt.Should().NotBeNull();

        var installation = await readDb.EdgeInstallations.SingleAsync(e => e.Id == installationId);
        installation.IsTokenConsumed.Should().BeTrue(); // InvalidateInstallToken — indisponível, sem fingir pareamento real
        installation.IsInstalled.Should().BeFalse();

        await using var consumeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var consumeProvider = MediatRTestContainerFactory.Build(consumeDb, new StaticTenantContext(tenantId));
        var consumeSender = consumeProvider.GetRequiredService<ISender>();

        var consume = await consumeSender.Send(new ConsumeInstallationTokenCommand(rawToken));
        consume.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeInstallationCredential_E_Idempotente()
    {
        var (tenantId, installationId, credentialId, _) = await SeedReissuedCredentialAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var first = await sender.Send(new RevokeInstallationCredentialCommand(
            installationId, credentialId, "Credencial possivelmente exposta", Guid.NewGuid()));
        first.IsSuccess.Should().BeTrue();

        await using var db2 = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider2 = MediatRTestContainerFactory.Build(db2, new StaticTenantContext(tenantId));
        var sender2 = provider2.GetRequiredService<ISender>();

        var second = await sender2.Send(new RevokeInstallationCredentialCommand(
            installationId, credentialId, "Revogando de novo por engano", Guid.NewGuid()));
        second.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeInstallationCredential_Inexistente_Retorna_404()
    {
        var (tenantId, installationId, _, _) = await SeedReissuedCredentialAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RevokeInstallationCredentialCommand(
            installationId, Guid.NewGuid(), "Credencial que não existe", Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.InstallationCredentialNotFound);
    }

    [Fact]
    public async Task RevokeInstallationCredential_Nao_Vaza_Segredo_No_AuditLog()
    {
        var (tenantId, installationId, credentialId, rawToken) = await SeedReissuedCredentialAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RevokeInstallationCredentialCommand(
            installationId, credentialId, "Credencial possivelmente exposta", Guid.NewGuid()));
        result.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var credential = await readDb.InstallationCredentials.SingleAsync(c => c.Id == credentialId);
        var auditLog = await readDb.AuditLogs
            .SingleAsync(a => a.Action == "INSTALLATION_CREDENTIAL_REVOKED" && a.EntityId == credentialId);

        auditLog.After.Should().NotContain(rawToken);
        auditLog.After.Should().NotContain(credential.TokenHash);
    }

    private async Task<(Guid TenantId, Guid InstallationId, Guid CredentialId, string RawToken)> SeedReissuedCredentialAsync()
    {
        var tenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

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
            installationId = installation.Id;
        }

        await using var db2 = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db2, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var reissue = await sender.Send(new ReissueInstallationTokenCommand(
            installationId, "Comando original não foi exibido", 24, Guid.NewGuid()));
        reissue.IsSuccess.Should().BeTrue();

        return (tenantId, installationId, reissue.Value!.CredentialId, reissue.Value!.InstallToken);
    }
}
