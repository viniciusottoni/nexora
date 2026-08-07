using Nexora.Application.Tenants.Commands.ProvisionTenant;
using Nexora.Application.Tenants.Commands.TransitionTenantStatus;
using Nexora.Application.Tenants.Commands.UpdateTenantPlan;
using Nexora.Application.Tenants.Queries.GetAdministrativeTimeline;
using Nexora.Application.Tenants.Support;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — prova o cenário Gherkin "Linha do
/// tempo administrativa" ("um tenant com mudanças de plano, status e proprietário... deve ver os
/// fatos em ordem cronológica, e cada item deve informar ator, origem, motivo e correlationId quando
/// aplicável") de ponta a ponta contra Postgres real, agregando TRÊS fontes diferentes (criação
/// sintética de <c>tenant.created_at</c>, <c>tenant_status_history</c> — US-153, e
/// <c>tenant_plan_history</c> — US-154) sem alterar nenhuma delas (RN-004).
/// </summary>
[Collection("Postgres")]
public sealed class AdministrativeTimelineIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public AdministrativeTimelineIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAdministrativeTimeline_Agrega_Criacao_Status_E_Plano_Em_Ordem_Cronologica_Com_Ator_Origem_Motivo_E_CorrelationId()
    {
        var slug = $"tenant-timeline-{Guid.NewGuid():N}";
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(new ProvisionTenantCommand(
            Name: "Pizzaria Dona Betinha",
            Slug: slug,
            Plan: "STANDARD",
            Template: "PIZZERIA",
            OwnerName: "Dona Betinha",
            OwnerEmail: $"owner-{Guid.NewGuid():N}@example.com",
            StoreName: "Matriz",
            StoreTimezone: "America/Sao_Paulo"));
        provisioned.IsSuccess.Should().BeTrue();
        var tenantId = provisioned.Value!.Tenant.Id;

        // PROVISIONED -> INSTALLING -> ACTIVE é sempre técnico (RegisterInstallationCommandHandler,
        // ver docstring de TenantStatusTransitions.AdminTargetsFrom — "nunca por decisão direta do
        // administrador"), então chegamos a ACTIVE direto pelo Domain (mesmo arranjo de teste que
        // TenantPlanIntegrationTests.MarkTenantActiveDirectly) para então exercitar uma transição que
        // o endpoint ADMINISTRATIVO realmente aceita (ACTIVE -> SUSPENDED) pelo pipeline MediatR real.
        await MarkTenantActiveDirectlyAsync(tenantId);

        await using var tenantDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var tenantProvider = MediatRTestContainerFactory.Build(tenantDb, new StaticTenantContext(tenantId));
        var tenantSender = tenantProvider.GetRequiredService<ISender>();

        var statusActorId = Guid.NewGuid();
        var toSuspended = await tenantSender.Send(new TransitionTenantStatusCommand(
            tenantId, "SUSPENDED", "Divergência comercial identificada pelo financeiro.", EffectiveAt: null, ExpectedVersion: 3, statusActorId));
        toSuspended.IsSuccess.Should().BeTrue();

        var planActorId = Guid.NewGuid();
        var planChange = await tenantSender.Send(new UpdateTenantPlanCommand(
            tenantId, "GESTAO", EffectiveAt: null, "Aditivo contratual #12", ExpectedVersion: 1, planActorId));
        planChange.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var readProvider = MediatRTestContainerFactory.Build(readDb, new StaticTenantContext(tenantId));
        var readSender = readProvider.GetRequiredService<ISender>();

        var timeline = await readSender.Send(new GetAdministrativeTimelineQuery(
            tenantId, Array.Empty<AdministrativeTimelineEntryType>(), From: null, To: null, Limit: 50, Cursor: null));

        timeline.IsSuccess.Should().BeTrue();
        var entries = timeline.Value!.Data;

        entries.Should().HaveCountGreaterOrEqualTo(3, "criação + transição de status + mudança de plano");
        entries.Select(e => e.OccurredAt).Should().BeInAscendingOrder("Gherkin: 'deve ver os fatos em ordem cronológica'");

        var creation = entries.Should().ContainSingle(e => e.Type == "CREATION").Subject;
        creation.Actor.Should().BeNull();
        creation.Origin.Should().Be("SYSTEM");

        var statusEntry = entries.Should().ContainSingle(e => e.Type == "STATUS_CHANGED").Subject;
        statusEntry.Actor.Should().NotBeNull();
        statusEntry.Actor!.Id.Should().Be(statusActorId);
        statusEntry.Reason.Should().Be("Divergência comercial identificada pelo financeiro.");
        statusEntry.Summary.Should().Contain("Ativo").And.Contain("Suspenso");
        statusEntry.CorrelationId.Should().NotBeNullOrEmpty("tenant.status_changed sempre emite evento correlacionado");

        var planEntry = entries.Should().ContainSingle(e => e.Type == "PLAN_CHANGED").Subject;
        planEntry.Actor.Should().NotBeNull();
        planEntry.Actor!.Id.Should().Be(planActorId);
        planEntry.Reason.Should().Be("Aditivo contratual #12");
        planEntry.Summary.Should().Contain("STANDARD").And.Contain("GESTAO");
        planEntry.CorrelationId.Should().NotBeNullOrEmpty("tenant.plan_changed sempre emite evento correlacionado quando a mudança é efetivada imediatamente");

        // Criação sempre é o fato mais antigo.
        entries[0].Type.Should().Be("CREATION");

        var onlyStatusActor = await readSender.Send(new GetAdministrativeTimelineQuery(
            tenantId,
            Array.Empty<AdministrativeTimelineEntryType>(),
            From: null,
            To: null,
            Limit: 50,
            Cursor: null,
            ActorId: statusActorId));
        onlyStatusActor.Value!.Data.Should().ContainSingle().Which.Type.Should().Be("STATUS_CHANGED");

        var onlyPlanCorrelation = await readSender.Send(new GetAdministrativeTimelineQuery(
            tenantId,
            Array.Empty<AdministrativeTimelineEntryType>(),
            From: null,
            To: null,
            Limit: 50,
            Cursor: null,
            CorrelationId: planEntry.CorrelationId));
        onlyPlanCorrelation.Value!.Data.Should().ContainSingle().Which.Type.Should().Be("PLAN_CHANGED");
    }

    [Fact]
    public async Task GetAdministrativeTimeline_Filtra_Por_Tipo_E_Por_Periodo()
    {
        var slug = $"tenant-timeline-{Guid.NewGuid():N}";
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(new ProvisionTenantCommand(
            Name: "Pizzaria Dona Betinha",
            Slug: slug,
            Plan: "STANDARD",
            Template: "PIZZERIA",
            OwnerName: "Dona Betinha",
            OwnerEmail: $"owner-{Guid.NewGuid():N}@example.com",
            StoreName: "Matriz",
            StoreTimezone: "America/Sao_Paulo"));
        var tenantId = provisioned.Value!.Tenant.Id;

        await MarkTenantActiveDirectlyAsync(tenantId);

        await using var tenantDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var tenantProvider = MediatRTestContainerFactory.Build(tenantDb, new StaticTenantContext(tenantId));
        var tenantSender = tenantProvider.GetRequiredService<ISender>();

        var transition = await tenantSender.Send(new TransitionTenantStatusCommand(
            tenantId, "SUSPENDED", "Instalação iniciada.", EffectiveAt: null, ExpectedVersion: 3, Guid.NewGuid()));
        transition.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var readProvider = MediatRTestContainerFactory.Build(readDb, new StaticTenantContext(tenantId));
        var readSender = readProvider.GetRequiredService<ISender>();

        var onlyStatus = await readSender.Send(new GetAdministrativeTimelineQuery(
            tenantId, new[] { AdministrativeTimelineEntryType.StatusChanged }, From: null, To: null, Limit: 50, Cursor: null));

        onlyStatus.IsSuccess.Should().BeTrue();
        onlyStatus.Value!.Data.Should().OnlyContain(e => e.Type == "STATUS_CHANGED");

        var future = DateTimeOffset.UtcNow.AddDays(1);
        var nothingInTheFuture = await readSender.Send(new GetAdministrativeTimelineQuery(
            tenantId, Array.Empty<AdministrativeTimelineEntryType>(), From: future, To: null, Limit: 50, Cursor: null));

        nothingInTheFuture.IsSuccess.Should().BeTrue();
        nothingInTheFuture.Value!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAdministrativeTimeline_Tenant_Inexistente_Retorna_404()
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetAdministrativeTimelineQuery(
            Guid.NewGuid(), Array.Empty<AdministrativeTimelineEntryType>(), null, null, 50, null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(Nexora.Shared.Errors.ApiErrorCodes.TenantNotFound);
    }

    /// <summary>Arranjo de teste — mesmo padrão de <c>TenantPlanIntegrationTests.MarkTenantActiveDirectly</c>: PROVISIONED -> INSTALLING -> ACTIVE direto pelo Domain (essa parte da máquina de estados é técnica, nunca administrativa — ver <c>TenantStatusTransitions.AdminTargetsFrom</c>).</summary>
    private async Task MarkTenantActiveDirectlyAsync(Guid tenantId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await db.Tenants.SingleAsync(t => t.Id == tenantId);
        var now = DateTimeOffset.UtcNow;
        tenant.TransitionStatus(Nexora.Domain.Platform.TenantStatus.Installing, now);
        tenant.TransitionStatus(Nexora.Domain.Platform.TenantStatus.Active, now);
        await db.SaveChangesAsync(CancellationToken.None);
    }
}
