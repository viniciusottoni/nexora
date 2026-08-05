using System.Text.Json;
using Nexora.Application.Onboarding.Commands.ActivateTenant;
using Nexora.Application.Onboarding.Commands.CompleteOnboardingStep;
using Nexora.Application.Onboarding.Queries.GetOnboardingStatus;
using Nexora.Application.Tenants.Commands.ProvisionTenant;
using Nexora.Application.Tables.Support;
using Nexora.Contracts.Platform;
using Nexora.Domain.Catalog;
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
/// US-141 (Provisionamento autoatendido) contra Postgres real com RLS ligado — cobre os cenários
/// Gherkin: "Checklist de implantação" (seed nos nove passos), "Autoatendimento pelo cliente"
/// (progresso avança conforme o estado real muda), "Validação antes da ativação" (bloqueio com lista
/// de pendências) e "Medição do tempo" (elapsedBusinessDays).
/// </summary>
[Collection("Postgres")]
public sealed class OnboardingIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public OnboardingIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário: "Checklist de implantação".</summary>
    [Fact]
    public async Task Provisionar_Tenant_Semeia_Os_Nove_Passos_Com_TenantCreated_Concluido()
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provisioned = await sender.Send(BuildProvisionCommand());
        provisioned.IsSuccess.Should().BeTrue();
        var tenantId = provisioned.Value!.Tenant.Id;

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var steps = await readDb.OnboardingSteps.Where(s => s.TenantId == tenantId).ToListAsync();

        steps.Should().HaveCount(9);
        steps.Single(s => s.Key == OnboardingStepKey.TenantCreated).Status.Should().Be(OnboardingStepStatus.Done);
        steps.Where(s => s.Key != OnboardingStepKey.TenantCreated).Should().OnlyContain(s => s.Status == OnboardingStepStatus.Pending);
    }

    /// <summary>Cenário: "Checklist de implantação" (via a query pública, com as chaves em formato de fio).</summary>
    [Fact]
    public async Task GetOnboardingStatus_Devolve_Os_Nove_Passos_Em_Formato_De_Fio_Com_StartedAt()
    {
        var (tenantId, provider) = await ProvisionAsync();
        await using var disposableProvider = provider;
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetOnboardingStatusQuery(tenantId));

        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;
        status.Steps.Should().HaveCount(9);
        status.Steps.Select(s => s.Key).Should().BeEquivalentTo(new[]
        {
            "TENANT_CREATED", "BRANDING", "MENU", "TABLES", "EDGE_INSTALL",
            "PAYMENT_CONFIG", "TRAINING", "PILOT", "ACTIVATION",
        });
        status.Steps.Single(s => s.Key == "TENANT_CREATED").Status.Should().Be("DONE");
        status.Steps.Single(s => s.Key == "MENU").Status.Should().Be("PENDING");
        status.StartedAt.Should().NotBeNull();
        status.ElapsedBusinessDays.Should().Be(0, "acabou de ser provisionado, no mesmo dia");
    }

    /// <summary>Cenário: "Autoatendimento pelo cliente" — cadastrar produtos avança MENU para IN_PROGRESS com contagem real.</summary>
    [Fact]
    public async Task Cadastrar_Produtos_Avanca_O_Passo_Menu_Para_InProgress_Com_Contagem_Real()
    {
        var (tenantId, provider) = await ProvisionAsync();
        await using var disposableProvider = provider;
        var sender = provider.GetRequiredService<ISender>();

        await using (var writeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var category = Category.Create(tenantId, "Pizzas Salgadas");
            writeDb.Categories.Add(category);
            await writeDb.SaveChangesAsync();

            for (var i = 0; i < 3; i++)
            {
                writeDb.Products.Add(Product.Create(tenantId, category.Id, $"Pizza {i}"));
            }
            await writeDb.SaveChangesAsync();
        }

        var result = await sender.Send(new GetOnboardingStatusQuery(tenantId));

        result.IsSuccess.Should().BeTrue();
        var menu = result.Value!.Steps.Single(s => s.Key == "MENU");
        menu.Status.Should().Be("IN_PROGRESS", "US-141 §7: catálogo com produtos mas sem meta confiável fica IN_PROGRESS, não DONE automaticamente");
        menu.Progress.Should().NotBeNull();
        menu.Progress!.Products.Should().Be(3);
    }

    /// <summary>Cenário: "Autoatendimento pelo cliente" — marca de identidade visual, mesas e edge instalado avançam os respectivos passos, visíveis à Replay.</summary>
    [Fact]
    public async Task Configurar_Marca_Mesas_E_Instalar_Edge_Conclui_Os_Passos_Correspondentes()
    {
        var (tenantId, provider) = await ProvisionAsync();
        await using var disposableProvider = provider;
        var sender = provider.GetRequiredService<ISender>();

        await using (var writeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var config = await writeDb.TenantConfigs.SingleAsync(c => c.TenantId == tenantId);
            config.UpdateBranding("""{"primaryColor":"#D62828","logoUrl":"https://cdn.example.com/logo.png"}""");

            var store = await writeDb.Stores.SingleAsync(s => s.TenantId == tenantId);
            var area = Area.Create(tenantId, store.Id, "Salão principal");
            writeDb.Areas.Add(area);
            await writeDb.SaveChangesAsync();

            writeDb.DiningTables.Add(DiningTable.Create(tenantId, store.Id, area.Id, "01", qrToken: Guid.NewGuid().ToString("N")));

            var edgeInstallation = await writeDb.EdgeInstallations.SingleAsync(e => e.TenantId == tenantId);
            edgeInstallation.MarkInstalled(publicKey: "test-public-key");

            await writeDb.SaveChangesAsync();
        }

        var result = await sender.Send(new GetOnboardingStatusQuery(tenantId));

        result.IsSuccess.Should().BeTrue();
        var steps = result.Value!.Steps.ToDictionary(s => s.Key, s => s.Status);
        steps["BRANDING"].Should().Be("DONE");
        steps["TABLES"].Should().Be("DONE");
        steps["EDGE_INSTALL"].Should().Be("DONE");
        steps["PAYMENT_CONFIG"].Should().Be("PENDING", "nenhum meio de pagamento foi configurado neste teste");
    }

    /// <summary>Cenário: "Validação antes da ativação" — ativação bloqueada com os pendentes listados.</summary>
    [Fact]
    public async Task Ativar_Com_Passos_Pendentes_E_Bloqueado_E_Lista_As_Pendencias()
    {
        var (tenantId, provider) = await ProvisionAsync();
        await using var disposableProvider = provider;
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ActivateTenantCommand(tenantId));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.OnboardingIncomplete);
        result.Errors.Should().NotBeNull();
        result.Errors!.Should().ContainKey(PendingItemsClosePolicy.MetaErrorsKey);

        var pendingJson = result.Errors![PendingItemsClosePolicy.MetaErrorsKey].Single();
        var pending = JsonSerializer.Deserialize<List<string>>(pendingJson)!;

        pending.Should().Contain(new[] { "BRANDING", "MENU", "TABLES", "EDGE_INSTALL", "PAYMENT_CONFIG", "TRAINING", "PILOT" });
        pending.Should().NotContain("TENANT_CREATED", "já nasce concluído");
        pending.Should().NotContain("ACTIVATION", "é o próprio passo sendo concluído por este comando, não uma pré-condição dele mesmo");

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.ActivatedAt.Should().BeNull();
        tenant.Status.Should().NotBe(TenantStatus.Active);
    }

    /// <summary>Cenário: "Validação antes da ativação" + "Medição do tempo" — com os oito passos concluídos, a ativação sucede e fecha a medição.</summary>
    [Fact]
    public async Task Ativar_Com_Todos_Os_Passos_Concluidos_Sucede_E_Marca_ActivatedAt()
    {
        var (tenantId, provider) = await ProvisionAsync();
        await using var disposableProvider = provider;
        var sender = provider.GetRequiredService<ISender>();

        await CompleteAllDerivableStepsAsync(tenantId);

        // TRAINING e PILOT não têm sinal automático — autoatendimento manual (US-141 §3.1).
        (await sender.Send(new CompleteOnboardingStepCommand(tenantId, OnboardingStepKey.Training, CompletedBy: Guid.NewGuid()))).IsSuccess.Should().BeTrue();
        (await sender.Send(new CompleteOnboardingStepCommand(tenantId, OnboardingStepKey.Pilot, CompletedBy: Guid.NewGuid()))).IsSuccess.Should().BeTrue();

        var activation = await sender.Send(new ActivateTenantCommand(tenantId));

        activation.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.ActivatedAt.Should().NotBeNull();

        var activationStep = await readDb.OnboardingSteps.SingleAsync(s => s.TenantId == tenantId && s.Key == OnboardingStepKey.Activation);
        activationStep.Status.Should().Be(OnboardingStepStatus.Done);
        activationStep.CompletedAt.Should().Be(tenant.ActivatedAt);
    }

    /// <summary>Cenário: "Medição do tempo" — dias úteis decorridos reflete a diferença real de calendário desde o início.</summary>
    [Fact]
    public async Task ElapsedBusinessDays_Reflete_Dias_Uteis_Desde_O_Inicio_Da_Implantacao()
    {
        var (tenantId, provider) = await ProvisionAsync();
        await using var disposableProvider = provider;
        var sender = provider.GetRequiredService<ISender>();

        // Backdata onboarding_started_at para uma segunda-feira conhecida, 7 dias corridos atrás
        // (5 dias úteis: seg/ter/qua/qui/sex) — tenant é a única tabela sem RLS, então este UPDATE
        // não depende de contexto de tenant.
        await using (var writeDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            var backdated = DateTimeOffset.UtcNow.AddDays(-7);
            await writeDb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE tenant SET onboarding_started_at = {backdated} WHERE id = {tenantId}");
        }

        var result = await sender.Send(new GetOnboardingStatusQuery(tenantId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ElapsedBusinessDays.Should().BeInRange(4, 5, "7 dias corridos atrás cruza um único fim de semana");
    }

    /// <summary>Passo TRAINING/PILOT concluído manualmente é persistido com quem concluiu.</summary>
    [Fact]
    public async Task CompleteOnboardingStep_Registra_Quem_Concluiu_Manualmente()
    {
        var (tenantId, provider) = await ProvisionAsync();
        await using var disposableProvider = provider;
        var sender = provider.GetRequiredService<ISender>();
        var actorId = Guid.NewGuid();

        var result = await sender.Send(new CompleteOnboardingStepCommand(tenantId, OnboardingStepKey.Training, actorId));

        result.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var step = await readDb.OnboardingSteps.SingleAsync(s => s.TenantId == tenantId && s.Key == OnboardingStepKey.Training);
        step.Status.Should().Be(OnboardingStepStatus.Done);
        step.CompletedBy.Should().Be(actorId);
    }

    /// <summary>PATCH manual não pode ser usado para concluir ACTIVATION — só ActivateTenantCommand pode.</summary>
    [Fact]
    public async Task CompleteOnboardingStep_Recusa_Concluir_Activation_Diretamente()
    {
        var (tenantId, provider) = await ProvisionAsync();
        await using var disposableProvider = provider;
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CompleteOnboardingStepCommand(tenantId, OnboardingStepKey.Activation, CompletedBy: null));

        result.IsSuccess.Should().BeFalse();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var step = await readDb.OnboardingSteps.SingleAsync(s => s.TenantId == tenantId && s.Key == OnboardingStepKey.Activation);
        step.Status.Should().Be(OnboardingStepStatus.Pending);
    }

    private async Task<(Guid TenantId, ServiceProvider Provider)> ProvisionAsync()
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provisionProvider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var provisionSender = provisionProvider.GetRequiredService<ISender>();

        var provisioned = await provisionSender.Send(BuildProvisionCommand());
        provisioned.IsSuccess.Should().BeTrue();
        var tenantId = provisioned.Value!.Tenant.Id;

        // A partir daqui, todo comando de onboarding roda no contexto do tenant recém-criado.
        var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var provider = MediatRTestContainerFactory.Build(readDb, new StaticTenantContext(tenantId));
        return (tenantId, provider);
    }

    /// <summary>Completa, por estado real, os cinco passos derivados (BRANDING/MENU/TABLES/EDGE_INSTALL/PAYMENT_CONFIG) — MENU precisa do PATCH manual, pois nunca fecha sozinho (ver teste de contagem parcial).</summary>
    private async Task CompleteAllDerivableStepsAsync(Guid tenantId)
    {
        await using var writeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var config = await writeDb.TenantConfigs.SingleAsync(c => c.TenantId == tenantId);
        config.UpdateBranding("""{"primaryColor":"#D62828"}""");
        // TenantConfig não tem um UpdatePayments dedicado hoje (só ApplyBootstrap substitui tudo de
        // uma vez, ADR-019) — reaproveita o próprio estado atual para as demais seções.
        config.ApplyBootstrap(
            config.ConfigVersion, config.CatalogVersion, config.Branding, config.Operation,
            config.Thresholds, config.Modules, config.Fiscal, config.Printers, """{"pix":true}""", config.Maintenance);

        var store = await writeDb.Stores.SingleAsync(s => s.TenantId == tenantId);
        var area = Area.Create(tenantId, store.Id, "Salão principal");
        writeDb.Areas.Add(area);
        await writeDb.SaveChangesAsync();
        writeDb.DiningTables.Add(DiningTable.Create(tenantId, store.Id, area.Id, "01", qrToken: Guid.NewGuid().ToString("N")));

        var edgeInstallation = await writeDb.EdgeInstallations.SingleAsync(e => e.TenantId == tenantId);
        edgeInstallation.MarkInstalled(publicKey: "test-public-key");

        var category = Category.Create(tenantId, "Pizzas Salgadas");
        writeDb.Categories.Add(category);
        await writeDb.SaveChangesAsync();
        writeDb.Products.Add(Product.Create(tenantId, category.Id, "Pizza Calabresa"));

        await writeDb.SaveChangesAsync();

        // MENU nunca fecha sozinho (sem meta confiável) — conclusão explícita, mesmo caminho do
        // autoatendimento real (assistente do painel do cliente, US-141 §3.1).
        await using var provider = MediatRTestContainerFactory.Build(writeDb, new StaticTenantContext(tenantId));
        var sender = provider.GetRequiredService<ISender>();
        (await sender.Send(new CompleteOnboardingStepCommand(tenantId, OnboardingStepKey.Menu, CompletedBy: Guid.NewGuid()))).IsSuccess.Should().BeTrue();
    }

    private static ProvisionTenantCommand BuildProvisionCommand() => new(
        Name: "Pizzaria Dona Betinha",
        Slug: $"tenant-{Guid.NewGuid():N}",
        Plan: "COMPLETO",
        Template: "PIZZERIA",
        OwnerName: "Dona Betinha",
        OwnerEmail: $"owner-{Guid.NewGuid():N}@example.com",
        StoreName: "Matriz",
        StoreTimezone: "America/Sao_Paulo");
}
