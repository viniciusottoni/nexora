using Nexora.Application.Tenants.Queries.GetTenantOverview;
using Nexora.Contracts.Tenants;
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
/// US-152 "Visão 360 e acesso aos módulos do estabelecimento" — <c>GetTenantOverviewQuery</c>
/// pelo pipeline MediatR real, contra Postgres real com RLS ligado (<see cref="PostgresFixture"/>,
/// mesma infraestrutura de <c>TenantDirectoryIntegrationTests</c>/<c>ProvisionTenantIntegrationTests</c>).
/// Cobre os quatro cenários Gherkin da US-152 §4 e a resiliência de seção exigida por §12
/// ("falha de uma seção não derruba o restante").
/// </summary>
[Collection("Postgres")]
public sealed class GetTenantOverviewIntegrationTests
{
    private const string DefaultDomainSuffix = "test.nexora.local";

    private readonly PostgresFixture _fixture;

    public GetTenantOverviewIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário: "Cadastro saudável".</summary>
    [Fact]
    public async Task Cadastro_Saudavel_Retorna_Todas_As_Secoes_Preenchidas_E_Checklist_Concluido()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Pizzaria {marker}", TenantStatus.Active);
        var ownerEmail = $"dono-{marker}@example.com";

        var roleId = await SeedOwnerRoleAsync(tenantId);
        await SeedOwnerWithCredentialAsync(tenantId, roleId, "Dona Betinha", ownerEmail);

        var storeId = await SeedStoreAsync(tenantId, "Matriz");
        await SeedInstalledEdgeInstallationAsync(tenantId, storeId, lastSeenAt: DateTimeOffset.UtcNow);

        await SeedOnboardingStepsAsync(tenantId, allDone: true);

        var result = await SendQueryAsync(tenantId);

        result.IsSuccess.Should().BeTrue();
        var value = result.Value!;

        value.Tenant.Id.Should().Be(tenantId);
        value.Tenant.Status.Should().Be("ACTIVE"); // wire format, nunca "Active" cru

        value.Owner.Should().NotBeNull();
        value.Owner!.Email.Should().Be(ownerEmail);
        value.Owner.InviteStatus.Should().Be("ACCEPTED");

        value.Stores.Should().ContainSingle().Which.Name.Should().Be("Matriz");

        var installation = value.Installations.Should().ContainSingle().Subject;
        installation.Status.Should().Be("ACTIVE");
        installation.Health.Should().Be("OK");

        value.Deployment.Completed.Should().Be(9);
        value.Deployment.Total.Should().Be(9);
        value.Deployment.NextAction.Should().BeNull();

        value.Links.PublicMenu.Should().NotBeNullOrWhiteSpace();
        value.Links.Admin.Should().NotBeNullOrWhiteSpace();
        value.Links.Health.Should().BeNull(); // deliberadamente nulo, US-152 §15
    }

    /// <summary>Cenário: "Provisionamento incompleto".</summary>
    [Fact]
    public async Task Provisionamento_Incompleto_Mostra_Instalacao_Pendente_E_Proxima_Acao_Segura()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Hamburgueria {marker}", TenantStatus.Provisioned);
        var ownerEmail = $"dono-{marker}@example.com";

        var roleId = await SeedOwnerRoleAsync(tenantId);
        await SeedInvitedOwnerAsync(tenantId, roleId, "Seu Zé", ownerEmail, consumed: false, expiresAt: DateTimeOffset.UtcNow.AddDays(3));

        var storeId = await SeedStoreAsync(tenantId, "Matriz");
        await SeedPendingEdgeInstallationAsync(tenantId, storeId);

        // TenantCreated (default Done) + Branding + Menu concluídos; Tables é o primeiro pendente.
        await SeedOnboardingStepsAsync(tenantId, doneKeys: new[] { OnboardingStepKey.Branding, OnboardingStepKey.Menu });

        var result = await SendQueryAsync(tenantId);

        result.IsSuccess.Should().BeTrue();
        var value = result.Value!;

        value.Owner.Should().NotBeNull();
        value.Owner!.InviteStatus.Should().Be("PENDING");

        var installation = value.Installations.Should().ContainSingle().Subject;
        installation.Status.Should().Be("PENDING");
        installation.Health.Should().Be("UNKNOWN"); // nunca DOWN por ausência de evidência

        value.Deployment.Completed.Should().Be(3);
        value.Deployment.Total.Should().Be(9);
        value.Deployment.NextAction.Should().Be("TABLES");
    }

    /// <summary>Convite expirado (não consumido, prazo vencido) → <c>inviteStatus: "EXPIRED"</c>.</summary>
    [Fact]
    public async Task Convite_Do_Proprietario_Expirado_E_Nao_Consumido_Retorna_Expired()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Estabelecimento {marker}", TenantStatus.Provisioned);
        var ownerEmail = $"dono-{marker}@example.com";

        var roleId = await SeedOwnerRoleAsync(tenantId);
        await SeedInvitedOwnerAsync(tenantId, roleId, "Proprietário", ownerEmail, consumed: false, expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        var result = await SendQueryAsync(tenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Owner.Should().NotBeNull();
        result.Value!.Owner!.InviteStatus.Should().Be("EXPIRED");
    }

    /// <summary>Cenário: "Recurso inexistente" — id nunca criado.</summary>
    [Fact]
    public async Task Tenant_Inexistente_Retorna_404_Sem_Informacao_Adicional()
    {
        var result = await SendQueryAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.TenantNotFound);
        result.Error.Should().Be("Estabelecimento não encontrado.");
        result.Errors.Should().BeNull();
    }

    /// <summary>Cenário: "Recurso inexistente" — id removido logicamente (soft delete).</summary>
    [Fact]
    public async Task Tenant_Removido_Logicamente_Retorna_404_Sem_Informacao_Adicional()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Estabelecimento {marker}", TenantStatus.Active);
        await SoftDeleteTenantAsync(tenantId);

        var result = await SendQueryAsync(tenantId);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.TenantNotFound);
        result.Errors.Should().BeNull();
    }

    /// <summary>
    /// Resiliência de seção (US-152 §12): tenant sem nenhum usuário no papel OWNER não pode
    /// derrubar a resposta inteira — só a seção <c>owner</c> fica nula, as demais seguem intactas.
    /// </summary>
    [Fact]
    public async Task Dono_Ausente_Nao_Derruba_As_Demais_Secoes_Da_Resposta()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Estabelecimento {marker}", TenantStatus.Active);

        var storeId = await SeedStoreAsync(tenantId, "Matriz");
        await SeedInstalledEdgeInstallationAsync(tenantId, storeId, lastSeenAt: DateTimeOffset.UtcNow);
        await SeedOnboardingStepsAsync(tenantId, allDone: false);

        var result = await SendQueryAsync(tenantId);

        result.IsSuccess.Should().BeTrue();
        var value = result.Value!;

        value.Owner.Should().BeNull();
        value.Stores.Should().ContainSingle();
        value.Installations.Should().ContainSingle();
        value.Deployment.Total.Should().Be(9);
    }

    private static string UniqueMarker() => $"qa{Guid.NewGuid():N}"[..14];

    private async Task<Guid> SeedTenantAsync(string name, TenantStatus status)
    {
        var tenantId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var tenant = Tenant.Create(tenantId, $"tenant-{tenantId:N}", name);

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

    private async Task SoftDeleteTenantAsync(Guid tenantId)
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var tenant = await db.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.SoftDelete();
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedOwnerRoleAsync(Guid tenantId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var role = Role.Create(tenantId, "OWNER", "Proprietário", isSystem: true);
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.Id;
    }

    /// <summary>Dono com credencial já ativa e SEM linha de convite — fallback defensivo "sem convite → ACCEPTED".</summary>
    private async Task SeedOwnerWithCredentialAsync(Guid tenantId, Guid roleId, string name, string email)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var user = AppUser.Create(tenantId, name, email, passwordHash: "hash-de-teste-nao-e-producao", pinHash: null);
        db.Users.Add(user);
        db.UserRoles.Add(UserRole.Create(tenantId, user.Id, roleId));
        await db.SaveChangesAsync();
    }

    /// <summary>Dono convidado (ainda sem credencial) com um <c>OwnerInvite</c> explícito, consumido ou não.</summary>
    private async Task SeedInvitedOwnerAsync(Guid tenantId, Guid roleId, string name, string email, bool consumed, DateTimeOffset expiresAt)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var user = AppUser.Invite(tenantId, name, email);
        db.Users.Add(user);
        db.UserRoles.Add(UserRole.Create(tenantId, user.Id, roleId));

        var invite = OwnerInvite.Create(tenantId, user.Id, email, secretHash: "secret-hash-teste", expiresAt: expiresAt);
        if (consumed)
        {
            invite.Consume();
        }

        db.OwnerInvites.Add(invite);
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedStoreAsync(Guid tenantId, string name)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var store = Store.Create(tenantId, name, isDefault: true);
        db.Stores.Add(store);
        await db.SaveChangesAsync();
        return store.Id;
    }

    /// <summary>Mesma técnica de <c>TenantDirectoryIntegrationTests.SeedInstalledEdgeInstallationAsync</c>: <c>last_seen_at</c> ajustado via SQL cru (o domínio não expõe setter para o passado).</summary>
    private async Task<Guid> SeedInstalledEdgeInstallationAsync(Guid tenantId, Guid storeId, DateTimeOffset lastSeenAt)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var installation = EdgeInstallation.CreateInstalled(
            Guid.NewGuid(), tenantId, storeId, "Servidor local — Matriz", publicKey: "pk-teste", version: "1.4.2");
        db.EdgeInstallations.Add(installation);
        await db.SaveChangesAsync();

        var appDb = (Nexora.Infrastructure.Persistence.AppDbContext)db;
        await appDb.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE edge_installation SET last_seen_at = {lastSeenAt} WHERE id = {installation.Id}");

        return installation.Id;
    }

    private async Task<Guid> SeedPendingEdgeInstallationAsync(Guid tenantId, Guid storeId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var installation = EdgeInstallation.Create(tenantId, storeId, "Servidor local — Matriz");
        db.EdgeInstallations.Add(installation);
        await db.SaveChangesAsync();
        return installation.Id;
    }

    /// <summary>
    /// Semeia os nove passos (<see cref="OnboardingStep.SeedAll"/> já marca <c>TenantCreated</c>
    /// como concluído) e completa também as chaves em <paramref name="doneKeys"/> — ou todas, se
    /// <paramref name="allDone"/>.
    /// </summary>
    private async Task SeedOnboardingStepsAsync(Guid tenantId, bool allDone = false, IReadOnlyCollection<OnboardingStepKey>? doneKeys = null)
    {
        var now = DateTimeOffset.UtcNow;
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var steps = OnboardingStep.SeedAll(tenantId, now);

        foreach (var step in steps)
        {
            if (step.Key == OnboardingStepKey.TenantCreated)
            {
                continue; // já vem Done do próprio SeedAll
            }

            if (allDone || (doneKeys?.Contains(step.Key) ?? false))
            {
                step.Complete(now, completedBy: null);
            }
        }

        db.OnboardingSteps.AddRange(steps);
        await db.SaveChangesAsync();
    }

    private async Task<Nexora.Application.Abstractions.Messaging.Result<TenantOverviewResponse>> SendQueryAsync(Guid tenantId)
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));

        var sender = provider.GetRequiredService<ISender>();
        return await sender.Send(new GetTenantOverviewQuery(tenantId));
    }
}
