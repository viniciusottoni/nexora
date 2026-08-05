using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tenants.Commands.ProvisionTenant;
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
/// Prova os cenários Gherkin da US-002 ("Criação de tenant a partir de modelo", "Slug duplicado",
/// "Criação transacional") de ponta a ponta — <c>ProvisionTenantCommand</c> pelo pipeline MediatR
/// real, contra Postgres real com RLS ligado (<see cref="PostgresFixture"/>, mesma infraestrutura
/// da US-001).
/// </summary>
/// <remarks>
/// O ator desta rota (admin de plataforma) não tem tenant próprio — por isso o handler precisa
/// fixar <c>app.tenant_id</c> explicitamente para o tenant recém-criado antes de inserir
/// store/role/station/etc. (ver <c>ProvisionTenantCommandHandler.Handle</c>,
/// <c>SetTenantContextAsync</c>). Sem esse fix, todo INSERT abaixo do <c>tenant</c> falharia a
/// política <c>tenant_isolation</c> (<c>WITH CHECK (tenant_id = current_tenant_id())</c>) — esta
/// suíte teria pegado a regressão na primeira execução contra Postgres real.
/// </remarks>
[Collection("Postgres")]
public sealed class ProvisionTenantIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public ProvisionTenantIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário: "Criação de tenant a partir de modelo".</summary>
    [Fact]
    public async Task ProvisionTenant_Cria_Tenant_Completo_Com_Seeds_Do_Template_Pizzaria()
    {
        var slug = UniqueSlug();
        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ProvisionTenantCommand(
            Name: "Pizzaria Dona Betinha",
            Slug: slug,
            Plan: "COMPLETO",
            Template: "PIZZERIA",
            OwnerName: "Dona Betinha",
            OwnerEmail: ownerEmail,
            StoreName: "Matriz",
            StoreTimezone: "America/Sao_Paulo"));

        result.IsSuccess.Should().BeTrue();
        var response = result.Value!;
        response.Tenant.Slug.Should().Be(slug);
        response.Tenant.Status.Should().Be("PROVISIONED");
        response.InstallToken.Should().NotBeNullOrWhiteSpace();
        response.InstallCommand.Should().Contain(response.Tenant.Id.ToString());
        response.OwnerInviteSentTo.Should().Be(ownerEmail);

        var tenantId = response.Tenant.Id;

        // A partir daqui, cada leitura precisa do contexto do tenant recém-criado (RLS real).
        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        (await readDb.Stores.CountAsync(s => s.TenantId == tenantId)).Should().Be(1);
        (await readDb.Roles.CountAsync(r => r.TenantId == tenantId)).Should().Be(7);
        (await readDb.Stations.CountAsync(s => s.TenantId == tenantId)).Should().Be(5);

        // US-017 §4, cenário "Praças padrão do modelo pizzaria": Forno é a única praça semeada
        // como gargalo — o forno determina o ritmo real da produção da pizzaria (RF-CAT-09).
        var stations = await readDb.Stations.Where(s => s.TenantId == tenantId).ToListAsync();
        stations.Where(s => s.IsBottleneck).Should().ContainSingle().Which.Type.Should().Be(StationType.Oven);

        // Gap corrigido nesta tarefa: expense_category/financial_account (Docs/Domain/12 §5/§6/§8).
        (await readDb.ExpenseCategories.CountAsync(e => e.TenantId == tenantId)).Should().Be(15);
        (await readDb.FinancialAccounts.CountAsync(f => f.TenantId == tenantId)).Should().Be(4);

        var owner = await readDb.Users.SingleAsync(u => u.TenantId == tenantId && u.Email == ownerEmail);
        owner.Status.Should().Be(UserStatus.Invited); // gap corrigido: era Inactive.

        (await readDb.UserRoles.CountAsync(ur => ur.TenantId == tenantId && ur.UserId == owner.Id)).Should().Be(1);
        (await readDb.EdgeInstallations.CountAsync(e => e.TenantId == tenantId)).Should().Be(1);
        (await readDb.OwnerInvites.CountAsync(i => i.TenantId == tenantId && i.UserId == owner.Id)).Should().Be(1);

        var outboxEntry = await readDb.EmailOutboxes.SingleAsync(e => e.TenantId == tenantId);
        outboxEntry.Recipient.Should().Be(ownerEmail);
        outboxEntry.Status.Should().Be("PENDING");

        (await readDb.DomainEvents.CountAsync(e => e.TenantId == tenantId && e.Type == "tenant.config_updated")).Should().Be(1);
        (await readDb.DomainEvents.CountAsync(e => e.TenantId == tenantId && e.Type == "permission.changed")).Should().Be(7);
    }

    /// <summary>Cenário: "Slug duplicado".</summary>
    [Fact]
    public async Task ProvisionTenant_Com_Slug_Duplicado_Retorna_422_E_Nao_Deixa_Registro_Parcial()
    {
        var slug = UniqueSlug();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var first = await sender.Send(BuildCommand(slug));
        first.IsSuccess.Should().BeTrue();

        var second = await sender.Send(BuildCommand(slug, ownerEmail: $"outro-{Guid.NewGuid():N}@example.com"));

        second.IsSuccess.Should().BeFalse();
        second.Code.Should().Be(ApiErrorCodes.TenantSlugAlreadyTaken);

        await using var readDb = _fixture.CreateAppDbContext(tenantContext: null);
        (await readDb.Tenants.CountAsync(t => t.Slug == slug)).Should().Be(1);
    }

    /// <summary>
    /// Cenário: "Criação transacional" — falha depois que várias entidades já foram adicionadas ao
    /// change tracker (papéis, praças, usuário, categorias, contas) reverte tudo: como
    /// <c>TransactionBehavior</c> só chama <c>SaveChangesAsync</c> uma única vez ao final do
    /// handler (ADR-006), uma exceção lançada antes desse ponto significa que NENHUM INSERT chega
    /// a ser enviado ao banco — nem o próprio <c>tenant</c>, que não tem RLS e seria visível sem
    /// nenhum contexto especial.
    /// </summary>
    [Fact]
    public async Task ProvisionTenant_Com_Falha_Na_Geracao_De_Token_Nao_Deixa_Nenhum_Registro_Parcial()
    {
        var slug = UniqueSlug();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));

        // Container dedicado com ISecretDigester substituído por um duplo que lança — representa
        // a etapa "geração de token" (do install token/convite) falhando, ainda antes do
        // SaveChangesAsync do TransactionBehavior.
        await using var brokenProvider = BuildContainerWithThrowingSecretDigester(db);
        var brokenSender = brokenProvider.GetRequiredService<ISender>();

        var act = async () => await brokenSender.Send(BuildCommand(slug));

        await act.Should().ThrowAsync<InvalidOperationException>();

        await using var readDb = _fixture.CreateAppDbContext(tenantContext: null);
        (await readDb.Tenants.CountAsync(t => t.Slug == slug)).Should().Be(0);
    }

    /// <summary>Cenário: "Isolamento" — dados semeados de um tenant não são visíveis a outro.</summary>
    [Fact]
    public async Task ProvisionTenant_Dados_Semeados_Do_Novo_Tenant_Nao_Sao_Visiveis_A_Outro_Tenant()
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var resultA = await sender.Send(BuildCommand(UniqueSlug()));
        var resultB = await sender.Send(BuildCommand(UniqueSlug()));
        resultA.IsSuccess.Should().BeTrue();
        resultB.IsSuccess.Should().BeTrue();

        var tenantAId = resultA.Value!.Tenant.Id;
        var tenantBId = resultB.Value!.Tenant.Id;

        await using var dbAsTenantB = _fixture.CreateAppDbContext(new StaticTenantContext(tenantBId));

        (await dbAsTenantB.Stations.AnyAsync(s => s.TenantId == tenantAId)).Should().BeFalse();
        (await dbAsTenantB.ExpenseCategories.AnyAsync(e => e.TenantId == tenantAId)).Should().BeFalse();
        (await dbAsTenantB.Users.AnyAsync(u => u.TenantId == tenantAId)).Should().BeFalse();

        // O tenant B só vê os próprios 5 stations/15 expense categories — nunca os do tenant A.
        (await dbAsTenantB.Stations.CountAsync()).Should().Be(5);
        (await dbAsTenantB.ExpenseCategories.CountAsync()).Should().Be(15);
    }

    private static ProvisionTenantCommand BuildCommand(string slug, string? ownerEmail = null) => new(
        Name: "Pizzaria Dona Betinha",
        Slug: slug,
        Plan: "COMPLETO",
        Template: "PIZZERIA",
        OwnerName: "Dona Betinha",
        OwnerEmail: ownerEmail ?? $"owner-{Guid.NewGuid():N}@example.com",
        StoreName: "Matriz",
        StoreTimezone: "America/Sao_Paulo");

    private static string UniqueSlug() => $"tenant-{Guid.NewGuid():N}";

    private static ServiceProvider BuildContainerWithThrowingSecretDigester(Nexora.Application.Abstractions.Persistence.IApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton<ICurrentTenantContext>(new StaticTenantContext(tenantId: null));
        services.AddSingleton<ISecretDigester>(new ThrowingSecretDigester());
        services.AddSingleton<Nexora.Application.Installations.Abstractions.IInstallationTokenDigester,
            Nexora.Infrastructure.Installations.InstallationTokenDigester>();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
            new Nexora.Infrastructure.Notifications.EmailOutboxOptions { EncryptionKey = "integration-test-key" }));
        services.AddSingleton<Nexora.Application.Abstractions.Notifications.IEmailSender,
            Nexora.Infrastructure.Notifications.EmailOutboxSender>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Nexora.Application.Abstractions.Messaging.ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(Nexora.Application.Abstractions.Behaviors.LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(Nexora.Application.Abstractions.Behaviors.TransactionBehavior<,>));
        });

        return services.BuildServiceProvider();
    }

    /// <summary>Duplo de teste que simula a etapa de geração de token falhando (Gherkin "Criação transacional").</summary>
    private sealed class ThrowingSecretDigester : ISecretDigester
    {
        public string Digest(string value) => throw new InvalidOperationException("Falha simulada na geração de token.");
    }
}
