using Nexora.Application.Installations.Commands.ReissueInstallationToken;
using Nexora.Application.Tenants.Commands.ProvisionTenant;
using Nexora.Application.Tenants.Queries.GetTenantDeploymentStatus;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — cenário Gherkin "Resposta de
/// criação foi perdida": o tenant/loja/instalação existem e o token original não foi consumido; o
/// administrador precisa VER isso reconstruído a partir de fatos persistidos (nunca de um cache da
/// tela de provisionamento que ele pode nem ter tido a chance de ver).
/// </summary>
[Collection("Postgres")]
public sealed class GetTenantDeploymentStatusIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public GetTenantDeploymentStatusIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Apos_Provisionar_Checklist_Mostra_Instalacao_Pendente_E_Permite_Reemissao()
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var provision = await sender.Send(new ProvisionTenantCommand(
            Name: "Pizzaria Dona Betinha",
            Slug: $"tenant-{Guid.NewGuid():N}",
            Plan: "COMPLETO",
            Template: "PIZZERIA",
            OwnerName: "Dona Betinha",
            OwnerEmail: $"owner-{Guid.NewGuid():N}@example.com",
            StoreName: "Matriz",
            StoreTimezone: "America/Sao_Paulo"));
        provision.IsSuccess.Should().BeTrue();
        var tenantId = provision.Value!.Tenant.Id;

        // Simula exatamente o cenário: a resposta do 201 (com o installToken) foi perdida — o
        // handler de deployment não depende dela em nada, só de EdgeInstallation/OnboardingStep já
        // persistidos.
        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var readProvider = MediatRTestContainerFactory.Build(readDb, new StaticTenantContext(tenantId));
        var readSender = readProvider.GetRequiredService<ISender>();

        var deployment = await readSender.Send(new GetTenantDeploymentStatusQuery(tenantId));

        deployment.IsSuccess.Should().BeTrue();
        var value = deployment.Value!;

        value.Total.Should().Be(9);
        value.Completed.Should().Be(1); // só TENANT_CREATED nasce concluído
        value.NextAction.Should().Be("BRANDING");
        value.Installation.Should().NotBeNull();
        value.Installation!.Status.Should().Be("PENDING");
        value.Installation.CanReissueToken.Should().BeTrue();

        // "deve poder reemitir um token sem duplicar tenant, loja ou instalação" — a reemissão
        // aponta para a MESMA instalação que o checklist acabou de reportar como pendente.
        var reissue = await readSender.Send(new ReissueInstallationTokenCommand(
            value.Installation.Id, "Comando original não foi exibido", 24, Guid.NewGuid()));
        reissue.IsSuccess.Should().BeTrue();

        (await readDb.EdgeInstallations.CountAsync()).Should().Be(1);
        (await readDb.Stores.CountAsync()).Should().Be(1);
    }
}
