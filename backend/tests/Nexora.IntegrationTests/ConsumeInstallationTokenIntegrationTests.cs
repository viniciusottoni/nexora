using Nexora.Application.Abstractions.Security;
using Nexora.Application.Installations.Commands.ConsumeInstallationToken;
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
/// Cenário Gherkin "Token de instalação de uso único" da US-002 — o mecanismo em si
/// (<c>ConsumeInstallationTokenCommandHandler</c>, módulo Installations) já existe e não foi
/// alterado por esta tarefa; a lacuna era a AUSÊNCIA de um teste provando o segundo uso contra
/// Postgres real. Usa <c>InstallationTokenDigester</c> real (SHA-256 puro, sem dependências) —
/// o mesmo algoritmo usado por <c>ProvisionTenantCommandHandler</c> ao gravar
/// <c>EdgeInstallation.InstallTokenHash</c> não entra aqui: o token semeado é inserido direto no
/// banco já com o hash calculado, sem passar pelo comando de provisionamento completo.
/// </summary>
[Collection("Postgres")]
public sealed class ConsumeInstallationTokenIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public ConsumeInstallationTokenIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConsumeInstallationToken_No_Segundo_Uso_E_Recusado_Com_403()
    {
        var tokenDigester = new Nexora.Infrastructure.Installations.InstallationTokenDigester();
        const string rawToken = "raw-install-token-de-teste";
        var tokenHash = tokenDigester.Digest(rawToken);

        var (tenantId, storeId) = await SeedTenantWithStoreAsync();

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var installation = EdgeInstallation.Create(tenantId, storeId, "Servidor local — Matriz");
            installation.IssueInstallToken(tokenHash, DateTimeOffset.UtcNow.AddHours(24));
            seedDb.EdgeInstallations.Add(installation);
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var first = await sender.Send(new ConsumeInstallationTokenCommand(rawToken));
        first.IsSuccess.Should().BeTrue();
        first.Value!.EdgeInstallationId.Should().NotBe(Guid.Empty);

        var second = await sender.Send(new ConsumeInstallationTokenCommand(rawToken));

        second.IsSuccess.Should().BeFalse();
        second.Code.Should().Be(ApiErrorCodes.InstallationTokenConflict);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var stored = await readDb.EdgeInstallations.SingleAsync(e => e.TenantId == tenantId);
        stored.IsTokenConsumed.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeInstallationToken_Expirado_E_Recusado_Com_403()
    {
        var tokenDigester = new Nexora.Infrastructure.Installations.InstallationTokenDigester();
        const string rawToken = "raw-install-token-expirado";
        var tokenHash = tokenDigester.Digest(rawToken);

        var (tenantId, storeId) = await SeedTenantWithStoreAsync();

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var installation = EdgeInstallation.Create(tenantId, storeId, "Servidor local — Matriz");
            installation.IssueInstallToken(tokenHash, DateTimeOffset.UtcNow.AddSeconds(-1));
            seedDb.EdgeInstallations.Add(installation);
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ConsumeInstallationTokenCommand(rawToken));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.InstallationTokenExpired);
    }

    private async Task<(Guid TenantId, Guid StoreId)> SeedTenantWithStoreAsync()
    {
        var tenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        Guid storeId;
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var store = Store.Create(tenantId, "Matriz", isDefault: true, timezone: "America/Sao_Paulo");
            db.Stores.Add(store);
            await db.SaveChangesAsync();
            storeId = store.Id;
        }

        return (tenantId, storeId);
    }
}
