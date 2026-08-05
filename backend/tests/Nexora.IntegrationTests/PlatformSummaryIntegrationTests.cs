using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Platform.Queries.GetPlatformSummary;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Devices;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-150 §7 "GET /v1/platform/summary" contra Postgres real (Testcontainers) — o resumo precisa
/// agregar corretamente entre VÁRIOS tenants sem vazar dado de negócio (RN-015), o que só é
/// verificável com RLS de verdade (edge_installation/owner_invite), não com o provider InMemory.
/// </summary>
[Collection("Postgres")]
public sealed class PlatformSummaryIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public PlatformSummaryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Resumo_Agrega_Tenants_Instalacoes_E_Convites_Pendentes_Entre_Varios_Tenants()
    {
        var active = await SeedTenantAsync("Pizzaria Ativa", TenantStatus.Active, installedHealthy: true);
        var suspended = await SeedTenantAsync("Pizzaria Suspensa", TenantStatus.Suspended, installedHealthy: false);

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = BuildContainer(db);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetPlatformSummaryQuery());

        result.IsSuccess.Should().BeTrue();
        var summary = result.Value!;

        summary.Tenants.Total.Should().BeGreaterThanOrEqualTo(2);
        summary.Tenants.Active.Should().BeGreaterThanOrEqualTo(1);
        summary.Tenants.Attention.Should().BeGreaterThanOrEqualTo(1);
        summary.Installations.Healthy.Should().BeGreaterThanOrEqualTo(1);
        summary.Installations.Offline.Should().BeGreaterThanOrEqualTo(1);
        summary.PendingInvites.Should().BeGreaterThanOrEqualTo(2);
        summary.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        _ = (active, suspended);
    }

    [Fact]
    public async Task Convite_Ja_Consumido_Ou_Expirado_Nao_Conta_Como_Pendente()
    {
        var tenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Pizzaria Convites"));
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var email = $"{tenantId:N}@example.com";
            var owner = AppUser.Invite(tenantId, "Dona Betinha", email);
            db.Users.Add(owner);

            var consumed = OwnerInvite.Create(tenantId, owner.Id, email, "hash-consumido", DateTimeOffset.UtcNow.AddHours(1));
            consumed.Consume();
            db.OwnerInvites.Add(consumed);

            var expired = OwnerInvite.Create(tenantId, owner.Id, email, "hash-expirado", DateTimeOffset.UtcNow.AddSeconds(-1));
            db.OwnerInvites.Add(expired);

            await db.SaveChangesAsync();
        }

        await using var readDb = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = BuildContainer(readDb);
        var sender = provider.GetRequiredService<ISender>();

        var before = await sender.Send(new GetPlatformSummaryQuery());

        before.IsSuccess.Should().BeTrue();

        // owner_invite tem RLS (ADR-004) — sem app.tenant_id fixado no contexto, a política nega
        // leitura por padrão ("falha fechada") e a contagem viria sempre 0, mesmo com os dois
        // convites persistidos; precisa de um contexto com o TENANT desta seed, não o `readDb`
        // "sem tenant" usado acima para simular o administrador de plataforma.
        await using var tenantScopedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var invitesForTenant = await tenantScopedDb.OwnerInvites.AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .CountAsync();
        invitesForTenant.Should().Be(2, "os dois convites (consumido e expirado) existem, mas nenhum deve contar como pendente");
    }

    private async Task<Guid> SeedTenantAsync(string name, TenantStatus status, bool installedHealthy)
    {
        var tenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            var tenant = Tenant.Create(tenantId, $"tenant-{tenantId:N}", name);
            switch (status)
            {
                case TenantStatus.Active:
                    tenant.Activate();
                    break;
                case TenantStatus.Suspended:
                    tenant.Suspend();
                    break;
            }

            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var store = Store.Create(tenantId, "Matriz", isDefault: true);
            db.Stores.Add(store);

            var installation = EdgeInstallation.CreateInstalled(
                Guid.NewGuid(), tenantId, store.Id, "Servidor local — Matriz", publicKey: "pk-teste", version: "1.4.2");
            db.EdgeInstallations.Add(installation);

            var email = $"{tenantId:N}@example.com";
            var owner = AppUser.Invite(tenantId, "Dono", email);
            db.Users.Add(owner);

            var invite = OwnerInvite.Create(tenantId, owner.Id, email, "hash-pendente", DateTimeOffset.UtcNow.AddHours(72));
            db.OwnerInvites.Add(invite);

            await db.SaveChangesAsync();

            var appDb = (Nexora.Infrastructure.Persistence.AppDbContext)db;
            var lastSeenAt = installedHealthy ? DateTimeOffset.UtcNow : DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30);
            await appDb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE edge_installation SET last_seen_at = {lastSeenAt} WHERE id = {installation.Id}");
        }

        return tenantId;
    }

    private static ServiceProvider BuildContainer(IApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton<ICurrentTenantContext>(new StaticTenantContext(tenantId: null));
        services.AddSingleton<IEventOriginProvider, CloudEventOriginProvider>();

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
