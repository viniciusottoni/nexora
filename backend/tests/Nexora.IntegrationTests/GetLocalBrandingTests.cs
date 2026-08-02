using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Branding.Queries.GetLocalBranding;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-003, gap "resolução de tenant por host não funciona para web-pos/web-kds" —
/// <see cref="GetLocalBrandingQuery"/> é o que <c>Nexora.Api.Edge.Controllers.BrandingController</c>
/// despacha em <c>GET /v1/local/branding</c>: diferente de <c>GetPublicBrandingQuery</c> (que
/// resolve por <c>Tenant.Domain</c>), aqui o tenant vem só de <c>ICurrentTenantContext.TenantId</c>
/// — o mesmo <see cref="StaticTenantContext"/> usado para simular a instalação fixa do edge
/// (ADR-004) nos outros testes desta classe de isolamento.
/// </summary>
[Collection("Postgres")]
public sealed class GetLocalBrandingTests
{
    private readonly PostgresFixture _fixture;

    public GetLocalBrandingTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetLocalBranding_Devolve_Branding_Do_Tenant_Da_Instalacao_Sem_Host()
    {
        var tenantId = Guid.NewGuid();
        const string brandingJson =
            """{"colors":{"primary":"#174A3B","secondary":"#D79232","surface":"#F4F0E8","onPrimary":"#FFFFFF"},"logo":{},"fonts":{"body":"Inter","display":"Fraunces"},"radius":10,"texts":{"welcome":"Bem-vindo","orderConfirmed":"","thanks":"","terms":""},"pwa":{"name":"Casa do Bairro","shortName":"Casa","themeColor":"#174A3B","icons":[]}}""";

        // Contexto do próprio tenant sendo semeado (não null): "tenant_config" tem RLS
        // (tenant_isolation, USING/WITH CHECK tenant_id = current_tenant_id()) — sem
        // "app.tenant_id" fixado para ESTE tenant, o INSERT seria recusado pelo banco. "tenant"
        // em si não tem RLS (não está na lista de tabelas de negócio), então usar o mesmo
        // contexto aqui é inofensivo.
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Casa do Bairro"));
            seedDb.TenantConfigs.Add(TenantConfig.CreateWithConfig(
                tenantId, brandingJson, "{}", "{}", "{}", "{}", "[]", "{}", "{}"));
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = BuildMediatRContainer(db, new StaticTenantContext(tenantId));

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(new GetLocalBrandingQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tenant.Id.Should().Be(tenantId);
        result.Value!.Tenant.Name.Should().Be("Casa do Bairro");
        result.Value!.Branding.Colors.Primary.Should().Be("#174A3B");
    }

    [Fact]
    public async Task GetLocalBranding_Sem_TenantId_No_Contexto_Falha_Fechado()
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(null));
        await using var provider = BuildMediatRContainer(db, new StaticTenantContext(null));

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(new GetLocalBrandingQuery());

        result.IsSuccess.Should().BeFalse();
    }

    private static ServiceProvider BuildMediatRContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services.BuildServiceProvider();
    }
}
