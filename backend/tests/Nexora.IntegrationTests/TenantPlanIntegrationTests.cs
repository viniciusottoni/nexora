using Nexora.Application.Tenants.Commands.ProvisionTenant;
using Nexora.Application.Tenants.Commands.ReconcileTenantPlanConfig;
using Nexora.Application.Tenants.Commands.UpdateTenantPlan;
using Nexora.Application.Tenants.Queries.GetTenantPlan;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — prova os quatro cenários Gherkin da US de
/// ponta a ponta, pelo pipeline MediatR real, contra Postgres real com RLS ligado (mesma
/// infraestrutura de <see cref="ProvisionTenantIntegrationTests"/>/<see cref="TransitionTenantStatusIntegrationTests"/>).
/// O catálogo <c>platform_plan</c> usado aqui é o mesmo semeado pela migration
/// <c>AddPlatformPlanCatalog</c> (STANDARD/GESTAO/COMPLETO) — nenhum teste cria plano novo, exceto
/// onde o próprio cenário exige um código ausente do catálogo.
/// </summary>
[Collection("Postgres")]
public sealed class TenantPlanIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public TenantPlanIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário: "Provisionamento preserva o plano solicitado".</summary>
    [Fact]
    public async Task ProvisionTenant_Com_Plano_Completo_Persiste_E_Nao_Substitui_Por_Standard()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(BuildProvisionCommand(slug, plan: "COMPLETO"));

        result.IsSuccess.Should().BeTrue();
        var tenantId = result.Value!.Tenant.Id;

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId);
        tenant.Plan.Should().Be("COMPLETO", "nenhuma camada deve substituir o plano confirmado por STANDARD");

        var config = await readDb.TenantConfigs.AsNoTracking().SingleAsync(c => c.TenantId == tenantId);
        config.AppliedPlanVersion.Should().Be(1);
    }

    [Fact]
    public async Task GetTenantPlan_Como_PlatformAdmin_Sem_Tenant_No_Token_Le_Config_Protegida_Por_Rls()
    {
        var slug = UniqueSlug();
        await using var provisionDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provisionProvider = MediatRTestContainerFactory.Build(
            provisionDb, new StaticTenantContext(tenantId: null));
        var provisionSender = provisionProvider.GetRequiredService<ISender>();
        var provisioned = await provisionSender.Send(BuildProvisionCommand(slug, plan: "COMPLETO"));
        var tenantId = provisioned.Value!.Tenant.Id;

        await using var platformDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var platformProvider = MediatRTestContainerFactory.Build(
            platformDb, new StaticTenantContext(tenantId: null));
        var platformSender = platformProvider.GetRequiredService<ISender>();

        var result = await platformSender.Send(new GetTenantPlanQuery(tenantId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.EffectiveCapabilities.Should().NotBeEmpty();
        result.Value.Consistent.Should().BeTrue(
            "o administrador de plataforma não carrega tenant_id no token, então o handler precisa fixar o contexto antes de consultar tenant_config");
    }

    [Fact]
    public async Task ReconcileTenantPlan_Como_PlatformAdmin_Sem_Tenant_No_Token_Le_Config_Protegida_Por_Rls()
    {
        var slug = UniqueSlug();
        await using var provisionDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provisionProvider = MediatRTestContainerFactory.Build(
            provisionDb, new StaticTenantContext(tenantId: null));
        var provisionSender = provisionProvider.GetRequiredService<ISender>();
        var provisioned = await provisionSender.Send(BuildProvisionCommand(slug, plan: "COMPLETO"));
        var tenantId = provisioned.Value!.Tenant.Id;

        await DivergeTenantConfigCapabilitiesAsync(tenantId);

        await using var platformDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var platformProvider = MediatRTestContainerFactory.Build(
            platformDb, new StaticTenantContext(tenantId: null));
        var platformSender = platformProvider.GetRequiredService<ISender>();

        var result = await platformSender.Send(new ReconcileTenantPlanConfigCommand(tenantId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue(
            "o administrador de plataforma não carrega tenant_id no token, então o handler precisa fixar o contexto antes de consultar tenant_config");
        result.Value!.Changed.Should().BeTrue();
        result.Value.Consistent.Should().BeTrue();
    }

    [Fact]
    public async Task GetTenantPlan_Detecta_Divergencia_De_Versao_Mesmo_Quando_Capacidades_Nao_Mudaram()
    {
        var slug = UniqueSlug();
        var planCode = $"TEST_{Guid.NewGuid():N}"[..21].ToUpperInvariant();
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null)))
        {
            seedDb.PlatformPlans.Add(PlatformPlan.Create(
                planCode, "Plano de teste", """["online_ordering"]""", "{}"));
            await seedDb.SaveChangesAsync(CancellationToken.None);
        }

        await using var provisionDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provisionProvider = MediatRTestContainerFactory.Build(
            provisionDb, new StaticTenantContext(tenantId: null));
        var provisionSender = provisionProvider.GetRequiredService<ISender>();
        var provisioned = await provisionSender.Send(BuildProvisionCommand(slug, plan: planCode));
        var tenantId = provisioned.Value!.Tenant.Id;

        await using (var catalogDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null)))
        {
            var catalogPlan = await catalogDb.PlatformPlans.SingleAsync(p => p.Code == planCode);
            catalogPlan.Update(catalogPlan.Name, catalogPlan.CapabilitiesJson, catalogPlan.LimitsJson);
            await catalogDb.SaveChangesAsync(CancellationToken.None);
        }

        await using var platformDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var platformProvider = MediatRTestContainerFactory.Build(
            platformDb, new StaticTenantContext(tenantId: null));
        var result = await platformProvider.GetRequiredService<ISender>().Send(new GetTenantPlanQuery(tenantId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Consistent.Should().BeFalse(
            "o catálogo é versionado e a versão aplicada faz parte da configuração efetiva, mesmo sem alteração no conjunto de capacidades");
    }

    /// <summary>Cenário: "Plano desconhecido" — no provisionamento.</summary>
    [Fact]
    public async Task ProvisionTenant_Com_Plano_Fora_Do_Catalogo_Retorna_422_Sem_Persistir_Nada()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(BuildProvisionCommand(slug, plan: "PLANO_INEXISTENTE"));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.PlanNotAvailable);

        await using var readDb = _fixture.CreateAppDbContext(tenantContext: null);
        (await readDb.Tenants.CountAsync(t => t.Slug == slug)).Should().Be(0, "nenhuma configuração parcial deve ser aplicada");
    }

    /// <summary>Cenário: "Mudança de plano com vigência".</summary>
    [Fact]
    public async Task UpdateTenantPlan_Com_Vigencia_Futura_Mantem_Plano_Atual_Ate_Data_Chegar_E_So_Entao_Emite_Evento()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(BuildProvisionCommand(slug, plan: "GESTAO"));
        provisioned.IsSuccess.Should().BeTrue();
        var tenantId = provisioned.Value!.Tenant.Id;

        await MarkTenantActiveDirectly(tenantId);

        await using var tenantDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var tenantProvider = MediatRTestContainerFactory.Build(tenantDb, new StaticTenantContext(tenantId));
        var tenantSender = tenantProvider.GetRequiredService<ISender>();

        var actorId = Guid.NewGuid();
        var effectiveAt = DateTimeOffset.UtcNow.AddDays(7);

        var scheduleResult = await tenantSender.Send(new UpdateTenantPlanCommand(
            tenantId, "COMPLETO", effectiveAt, "Aditivo contratual #32", ExpectedVersion: 1, actorId));

        scheduleResult.IsSuccess.Should().BeTrue();
        scheduleResult.Value!.Current.Should().Be("GESTAO", "o plano atual deve permanecer até a vigência");
        scheduleResult.Value.Scheduled.Should().NotBeNull();
        scheduleResult.Value.Scheduled!.Plan.Should().Be("COMPLETO");

        await using var afterScheduleDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        (await afterScheduleDb.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId)).Plan.Should().Be("GESTAO");

        var history = await afterScheduleDb.TenantPlanHistories.AsNoTracking()
            .SingleAsync(h => h.TenantId == tenantId);
        history.PreviousPlan.Should().Be("GESTAO");
        history.NextPlan.Should().Be("COMPLETO");
        history.IsPending.Should().BeTrue("a mudança deve aparecer no histórico mesmo antes da vigência");

        (await afterScheduleDb.DomainEvents.CountAsync(e => e.TenantId == tenantId && e.Type == "tenant.plan_changed"))
            .Should().Be(0, "o evento só é emitido na efetivação, nunca no agendamento");

        // Simula a vigência já ter chegado (o teste não espera 7 dias reais) — move a data efetiva
        // da linha de histórico para o passado, diretamente via SQL, e deixa a PRÓXIMA leitura do
        // plano (GetTenantPlanQueryHandler) efetivá-la de forma preguiçosa/idempotente.
        await SetHistoryEffectiveAtToPastAsync(tenantId, history.Id);

        await using var lazyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var lazyProvider = MediatRTestContainerFactory.Build(lazyDb, new StaticTenantContext(tenantId));
        var lazySender = lazyProvider.GetRequiredService<ISender>();

        var planAfterEffectuation = await lazySender.Send(new GetTenantPlanQuery(tenantId));

        planAfterEffectuation.IsSuccess.Should().BeTrue();
        planAfterEffectuation.Value!.Current.Should().Be("COMPLETO");
        planAfterEffectuation.Value.Scheduled.Should().BeNull();
        planAfterEffectuation.Value.Consistent.Should().BeTrue();
        planAfterEffectuation.Value.History.Should().ContainSingle();
        planAfterEffectuation.Value.History[0].Should().BeEquivalentTo(new
        {
            Previous = "GESTAO",
            Next = "COMPLETO",
            Reason = "Aditivo contratual #32",
            ActorId = (Guid?)actorId,
        });
        planAfterEffectuation.Value.History[0].AppliedAt.Should().NotBeNull();

        await using var finalDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        (await finalDb.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId)).Plan.Should().Be("COMPLETO");
        (await finalDb.TenantPlanHistories.AsNoTracking().SingleAsync(h => h.TenantId == tenantId)).AppliedAt.Should().NotBeNull();
        (await finalDb.DomainEvents.CountAsync(e => e.TenantId == tenantId && e.Type == "tenant.plan_changed")).Should().Be(1);
        (await finalDb.AuditLogs.CountAsync(a => a.TenantId == tenantId && a.Action == "TENANT_PLAN_CHANGED")).Should().Be(1);

        // Chamar de novo não duplica o evento (idempotência da efetivação preguiçosa).
        var second = await lazySender.Send(new GetTenantPlanQuery(tenantId));
        second.IsSuccess.Should().BeTrue();
        (await finalDb.DomainEvents.CountAsync(e => e.TenantId == tenantId && e.Type == "tenant.plan_changed")).Should().Be(1);
    }

    /// <summary>Cenário: "Plano desconhecido" — na mudança de plano de um tenant existente.</summary>
    [Fact]
    public async Task UpdateTenantPlan_Com_Codigo_Ausente_Do_Catalogo_Retorna_422_Sem_Aplicar_Nada()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(BuildProvisionCommand(slug, plan: "GESTAO"));
        var tenantId = provisioned.Value!.Tenant.Id;

        await using var tenantDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var tenantProvider = MediatRTestContainerFactory.Build(tenantDb, new StaticTenantContext(tenantId));
        var tenantSender = tenantProvider.GetRequiredService<ISender>();

        var result = await tenantSender.Send(new UpdateTenantPlanCommand(
            tenantId, "PLANO_INEXISTENTE", null, "Teste", ExpectedVersion: 1, Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.PlanNotAvailable);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        (await readDb.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId)).Plan.Should().Be("GESTAO");
        (await readDb.TenantPlanHistories.CountAsync(h => h.TenantId == tenantId)).Should().Be(0, "nenhuma configuração parcial deve ser aplicada");
    }

    /// <summary>Cenário: "Divergência detectada".</summary>
    [Fact]
    public async Task ReconcileTenantPlanConfig_Corrige_Divergencia_De_Forma_Idempotente_E_Auditada()
    {
        var slug = UniqueSlug();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(BuildProvisionCommand(slug, plan: "COMPLETO"));
        var tenantId = provisioned.Value!.Tenant.Id;

        // Simula divergência: capacidades efetivas gravadas manualmente diferentes do catálogo do
        // plano corrente — cenário realista de um tenant provisionado antes desta história, ou de
        // um catálogo atualizado depois da última reconciliação.
        await DivergeTenantConfigCapabilitiesAsync(tenantId);

        await using var tenantDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var tenantProvider = MediatRTestContainerFactory.Build(tenantDb, new StaticTenantContext(tenantId));
        var tenantSender = tenantProvider.GetRequiredService<ISender>();

        var planView = await tenantSender.Send(new GetTenantPlanQuery(tenantId));
        planView.IsSuccess.Should().BeTrue();
        planView.Value!.Consistent.Should().BeFalse("a divergência deve ser detectada, nunca corrigida silenciosamente pela leitura");

        var actorId = Guid.NewGuid();
        var reconcile = await tenantSender.Send(new ReconcileTenantPlanConfigCommand(tenantId, actorId));

        reconcile.IsSuccess.Should().BeTrue();
        reconcile.Value!.Changed.Should().BeTrue();
        reconcile.Value.Consistent.Should().BeTrue();

        await using var afterDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        (await afterDb.AuditLogs.CountAsync(a => a.TenantId == tenantId && a.Action == "TENANT_PLAN_CONFIG_RECONCILED")).Should().Be(1);
        (await afterDb.DomainEvents.CountAsync(e => e.TenantId == tenantId && e.Type == "tenant.config_updated" && e.ActorId == actorId)).Should().Be(1);

        // Idempotência — reconciliar de novo não muda nada nem duplica auditoria/evento.
        var second = await tenantSender.Send(new ReconcileTenantPlanConfigCommand(tenantId, actorId));
        second.IsSuccess.Should().BeTrue();
        second.Value!.Changed.Should().BeFalse();

        (await afterDb.AuditLogs.CountAsync(a => a.TenantId == tenantId && a.Action == "TENANT_PLAN_CONFIG_RECONCILED")).Should().Be(1);
    }

    private static ProvisionTenantCommand BuildProvisionCommand(string slug, string plan) => new(
        Name: "Pizzaria Dona Betinha",
        Slug: slug,
        Plan: plan,
        Template: "PIZZERIA",
        OwnerName: "Dona Betinha",
        OwnerEmail: $"owner-{Guid.NewGuid():N}@example.com",
        StoreName: "Matriz",
        StoreTimezone: "America/Sao_Paulo");

    private static string UniqueSlug() => $"tenant-plan-{Guid.NewGuid():N}";

    /// <summary>Arranjo de teste — move o tenant para ACTIVE direto pelo Domain (não é o alvo deste teste, só um pré-requisito do cenário Gherkin "tenant ACTIVE no plano GESTAO").</summary>
    private async Task MarkTenantActiveDirectly(Guid tenantId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await db.Tenants.SingleAsync(t => t.Id == tenantId);
        var now = DateTimeOffset.UtcNow;
        tenant.TransitionStatus(Nexora.Domain.Platform.TenantStatus.Installing, now);
        tenant.TransitionStatus(Nexora.Domain.Platform.TenantStatus.Active, now);
        await db.SaveChangesAsync(CancellationToken.None);
    }

    /// <summary>
    /// RLS (ADR-004) protege <c>tenant_plan_history</c>/<c>tenant_config</c> mesmo para este UPDATE
    /// direto de arranjo de teste — sem fixar <c>app.tenant_id</c> na sessão (mesma GUC que
    /// <c>TenantConnectionInterceptor</c> define via <c>SET LOCAL</c> em produção), a política
    /// <c>tenant_isolation</c> simplesmente não encontraria a linha ("falha fechada").
    /// </summary>
    private async Task SetHistoryEffectiveAtToPastAsync(Guid tenantId, Guid historyId)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var setTenant = connection.CreateCommand();
        setTenant.CommandText = $"SET app.tenant_id = '{tenantId}'";
        await setTenant.ExecuteNonQueryAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE tenant_plan_history SET effective_at = now() - interval '1 hour' WHERE id = @id";
        cmd.Parameters.AddWithValue("id", historyId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task DivergeTenantConfigCapabilitiesAsync(Guid tenantId)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var setTenant = connection.CreateCommand();
        setTenant.CommandText = $"SET app.tenant_id = '{tenantId}'";
        await setTenant.ExecuteNonQueryAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE tenant_config SET plan_capabilities = '[]'::jsonb, applied_plan_version = NULL WHERE tenant_id = @id";
        cmd.Parameters.AddWithValue("id", tenantId);
        await cmd.ExecuteNonQueryAsync();
    }
}
