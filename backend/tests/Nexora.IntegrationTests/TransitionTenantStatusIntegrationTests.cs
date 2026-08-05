using Nexora.Application.Tenants.Commands.TransitionTenantStatus;
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
/// US-153 "Ciclo de vida do estabelecimento" contra Postgres real — cobre os cenários Gherkin
/// "Suspensão administrativa", "Transição inválida" e "Concorrência" (doc §4/§12: "transação,
/// histórico, evento, idempotência e concorrência"; a idempotência de <c>Idempotency-Key</c> em si é
/// coberta genericamente por <c>IdempotencyMiddlewareTests</c>/<c>IdempotencyStoreTests</c> — todo
/// endpoint de escrita passa pelo mesmo middleware, não precisa de um teste próprio aqui). Mesmo
/// padrão de <see cref="SupportAccessAuditTests"/>: o ator (admin de plataforma) não tem tenant
/// próprio, então <see cref="StaticTenantContext"/> é criado com <c>tenantId: null</c>.
/// </summary>
[Collection("Postgres")]
public sealed class TransitionTenantStatusIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public TransitionTenantStatusIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Suspender_Tenant_Active_Grava_Historico_Evento_AuditLog_E_Muda_Status()
    {
        var tenantId = await SeedTenantAsync(TenantStatus.Active);
        var actorId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new TransitionTenantStatusCommand(
            tenantId, "SUSPENDED", "Solicitação contratual #482", EffectiveAt: null, ExpectedVersion: 1, actorId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.PreviousStatus.Should().Be("ACTIVE");
        result.Value!.Status.Should().Be("SUSPENDED");
        result.Value!.Version.Should().Be(2);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Status.Should().Be(TenantStatus.Suspended);
        tenant.StatusVersion.Should().Be(2);

        var history = await readDb.TenantStatusHistories.SingleAsync(h => h.TenantId == tenantId);
        history.PreviousStatus.Should().Be(TenantStatus.Active);
        history.NewStatus.Should().Be(TenantStatus.Suspended);
        history.Reason.Should().Be("Solicitação contratual #482");
        history.ActorId.Should().Be(actorId);
        history.Origin.Should().Be("PLATFORM_ADMIN");
        history.DomainEventId.Should().NotBeNull();

        var domainEvent = await readDb.DomainEvents.SingleAsync(e => e.Id == history.DomainEventId);
        domainEvent.Type.Should().Be("tenant.status_changed");
        domainEvent.TenantId.Should().Be(tenantId);

        var auditEntry = await readDb.AuditLogs.SingleAsync(a => a.TenantId == tenantId && a.Action == "TENANT_STATUS_CHANGED");
        auditEntry.ActorId.Should().Be(actorId);
        auditEntry.Reason.Should().Be("Solicitação contratual #482");
    }

    [Fact]
    public async Task Reativar_Tenant_Suspended_Sucede_E_Marca_ActivatedAt()
    {
        var tenantId = await SeedTenantAsync(TenantStatus.Suspended);

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new TransitionTenantStatusCommand(
            tenantId, "ACTIVE", "Pendência contratual resolvida", EffectiveAt: null, ExpectedVersion: 1, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.ActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Ativar_Tenant_Cancelled_Diretamente_Retorna_409_Sem_Persistir_Nada()
    {
        var tenantId = await SeedTenantAsync(TenantStatus.Cancelled);

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new TransitionTenantStatusCommand(
            tenantId, "ACTIVE", "Tentativa indevida", EffectiveAt: null, ExpectedVersion: 1, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.TenantStatusTransitionInvalid);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Status.Should().Be(TenantStatus.Cancelled, "nenhum estado parcial deve ser persistido");
        tenant.StatusVersion.Should().Be(1);
        (await readDb.TenantStatusHistories.AnyAsync(h => h.TenantId == tenantId)).Should().BeFalse();
    }

    [Fact]
    public async Task Versao_Divergente_No_If_Match_Retorna_409_De_Concorrencia_Sem_Persistir()
    {
        var tenantId = await SeedTenantAsync(TenantStatus.Active);

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new TransitionTenantStatusCommand(
            tenantId, "SUSPENDED", "Motivo válido", EffectiveAt: null, ExpectedVersion: 999, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.ConcurrencyConflict);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.StatusVersion.Should().Be(1);
    }

    [Fact]
    public async Task Motivo_Vazio_Retorna_422_Sem_Persistir()
    {
        var tenantId = await SeedTenantAsync(TenantStatus.Active);

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new TransitionTenantStatusCommand(
            tenantId, "SUSPENDED", "   ", EffectiveAt: null, ExpectedVersion: 1, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.ReasonRequired);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.StatusVersion.Should().Be(1);
    }

    [Fact]
    public async Task Tenant_Inexistente_Retorna_404()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new TransitionTenantStatusCommand(
            Guid.NewGuid(), "SUSPENDED", "Motivo válido", EffectiveAt: null, ExpectedVersion: 1, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.TenantNotFound);
    }

    private async Task<Guid> SeedTenantAsync(TenantStatus status)
    {
        var tenantId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var tenant = Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant");

        switch (status)
        {
            case TenantStatus.Active:
                tenant.Activate();
                break;
            case TenantStatus.Suspended:
                tenant.Suspend();
                break;
            case TenantStatus.Cancelled:
                tenant.Cancel();
                break;
        }

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        return tenantId;
    }
}
