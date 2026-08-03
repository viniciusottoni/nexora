using Nexora.Application.Roles.Commands.UpdateRole;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// E-09/US-090, ação "alteração de permissão" do RF-AUD-02 — fechava o único gap de cobertura
/// funcional entre as 4 ações já implementadas (as outras três — cancelamento, desconto/autorização
/// e alteração de preço — já têm teste de integração dedicado com asserção sobre
/// <c>audit_log</c> em <c>CancelOrderIntegrationTests</c>/<c>PricingIntegrationTests</c>; ver
/// também o inventário em <c>AuditCoverageTests</c>). Usa <see cref="MediatRTestContainerFactory"/>
/// (pipeline completo de produção: Validation -&gt; Logging -&gt; Transaction), não um container
/// MediatR próprio.
/// </summary>
[Collection("Postgres")]
public sealed class RoleAuditTests
{
    private readonly PostgresFixture _fixture;

    public RoleAuditTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateRole_Com_Permissoes_Alteradas_Grava_Audit_Log_E_Domain_Event_Correlacionados()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        Guid roleId;

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant"));
            await seedDb.SaveChangesAsync();
        }

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var role = Role.Create(tenantId, "WAITER", "Garçom");
            role.UpdatePermissions("[\"order:read\"]");
            seedDb.Roles.Add(role);
            await seedDb.SaveChangesAsync();
            roleId = role.Id;
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, userId: actorId));

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(new UpdateRoleCommand(roleId, Name: null, Permissions: new[] { "order:read", "order:cancel_queued" }));

        result.IsSuccess.Should().BeTrue();

        var auditEntry = await db.AuditLogs.SingleAsync(a => a.TenantId == tenantId && a.EntityId == roleId);
        auditEntry.Action.Should().Be("PERMISSION_CHANGED");
        auditEntry.ActorId.Should().Be(actorId);
        auditEntry.DomainEventId.Should().NotBeNull();

        var domainEvent = await db.DomainEvents.SingleAsync(e => e.Id == auditEntry.DomainEventId);
        domainEvent.Type.Should().Be("permission.changed");
        domainEvent.AggregateId.Should().Be(roleId);
    }
}
