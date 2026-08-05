using Nexora.Application.Branding.Queries.GetPublicBranding;
using Nexora.Application.TenantDomains.Commands.RegisterTenantDomain;
using Nexora.Application.TenantDomains.Commands.RenewTenantDomainCertificate;
using Nexora.Application.TenantDomains.Commands.VerifyTenantDomain;
using Nexora.Application.TenantDomains.Queries.ListTenantDomains;
using Nexora.Application.Tenants.Commands.ProvisionTenant;
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
/// Prova os cenários Gherkin da US-143 ("Verificação de propriedade", "Certificado automático",
/// "Resolução de tenant pelo host", "Domínio não verificado") de ponta a ponta contra Postgres
/// real (RLS ligado). Também prova a decisão de projeto registrada no relatório desta tarefa:
/// como <c>tenant_domain</c> tem RLS (ao contrário de <c>tenant</c>) e os endpoints públicos de
/// branding/cardápio resolvem tenant ANTES de qualquer contexto existir, a verificação bem-sucedida
/// espelha o domínio em <c>Tenant.Domain</c> (<see cref="Nexora.Domain.Platform.Tenant.SetCustomDomain"/>)
/// — por isso <see cref="GetPublicBrandingQuery"/> (mecanismo herdado da US-003, NÃO modificado por
/// esta tarefa) já resolve o domínio próprio sem precisar de nenhuma mudança nele.
/// </summary>
[Collection("Postgres")]
public sealed class TenantDomainIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public TenantDomainIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenários: "Verificação de propriedade", "Certificado automático", "Resolução de tenant pelo host".</summary>
    [Fact]
    public async Task Registrar_Depois_Verificar_Com_Registro_Dns_Presente_Ativa_Emite_Certificado_E_Resolve_Pelo_Host()
    {
        var domainName = UniqueDomain();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(
            db, new StaticTenantContext(tenantId: null), domainVerificationService: new FakeDomainVerificationService(result: true));
        var sender = provider.GetRequiredService<ISender>();

        var tenantId = await ProvisionAsync(sender);

        var registered = await sender.Send(new RegisterTenantDomainCommand(tenantId, domainName));
        registered.IsSuccess.Should().BeTrue();
        registered.Value!.Status.Should().Be("PENDING_VERIFICATION");
        registered.Value.Verification.Type.Should().Be("TXT");
        registered.Value.Verification.Name.Should().Be($"_verify.{domainName}");
        registered.Value.Verification.Value.Should().NotBeNullOrWhiteSpace();

        // "Domínio não verificado ainda" — o mecanismo de US-003 NÃO deve resolver antes da verificação.
        var beforeVerify = await sender.Send(new GetPublicBrandingQuery(domainName));
        beforeVerify.IsSuccess.Should().BeFalse();

        var domainId = registered.Value.Domain.Id;
        var verified = await sender.Send(new VerifyTenantDomainCommand(domainId));

        verified.IsSuccess.Should().BeTrue();
        verified.Value!.Domain.Status.Should().Be("ACTIVE");
        verified.Value.Domain.IsPrimary.Should().BeTrue();
        verified.Value.Domain.CertStatus.Should().Be("ISSUED");
        verified.Value.Domain.CertExpiresAt.Should().NotBeNull();
        verified.Value.Domain.CertExpiresAt!.Value.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(80));

        // Cenário "Resolução de tenant pelo host" — mesmo mecanismo da US-003
        // (GetPublicBrandingQueryHandler, não tocado por esta tarefa), agora resolvendo o domínio
        // próprio porque a verificação o espelhou em Tenant.Domain.
        var afterVerify = await sender.Send(new GetPublicBrandingQuery(domainName));
        afterVerify.IsSuccess.Should().BeTrue();
        afterVerify.Value!.Tenant.Id.Should().Be(tenantId);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var tenant = await readDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Domain.Should().Be(domainName);
    }

    /// <summary>Cenário: "Domínio não verificado" — falha com instruções claras do que fazer, e não ativa nada.</summary>
    [Fact]
    public async Task Verificar_Sem_Registro_Dns_Falha_Com_Instrucoes_E_Nao_Ativa_O_Dominio()
    {
        var domainName = UniqueDomain();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var registerProvider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var registerSender = registerProvider.GetRequiredService<ISender>();

        var tenantId = await ProvisionAsync(registerSender);
        var registered = await registerSender.Send(new RegisterTenantDomainCommand(tenantId, domainName));
        registered.IsSuccess.Should().BeTrue();
        var domainId = registered.Value!.Domain.Id;
        var expectedRecordName = registered.Value.Verification.Name;
        var expectedRecordValue = registered.Value.Verification.Value;

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var verifyProvider = MediatRTestContainerFactory.Build(
            verifyDb, new StaticTenantContext(tenantId: null), domainVerificationService: new FakeDomainVerificationService(result: false));
        var verifySender = verifyProvider.GetRequiredService<ISender>();

        var verified = await verifySender.Send(new VerifyTenantDomainCommand(domainId));

        verified.IsSuccess.Should().BeFalse();
        verified.Code.Should().Be(ApiErrorCodes.TenantDomainVerificationFailed);
        verified.Error.Should().Contain(expectedRecordName);
        verified.Error.Should().Contain(expectedRecordValue);
        verified.Error.Should().Contain(domainName);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var domain = await readDb.TenantDomains.SingleAsync(d => d.Id == domainId);
        domain.Status.Should().Be(Nexora.Domain.Platform.TenantDomainStatus.PendingVerification);
        domain.VerifiedAt.Should().BeNull();

        var tenant = await readDb.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Domain.Should().BeNull();
    }

    /// <summary>Cenário/nível "Isolamento": domínio de um tenant não resolve outro, e o cadastro global é único (RN-015).</summary>
    [Fact]
    public async Task Dominio_De_Um_Tenant_Nao_Pode_Ser_Registrado_Por_Outro_Nem_Resolve_O_Outro()
    {
        var domainName = UniqueDomain();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var provider = MediatRTestContainerFactory.Build(
            db, new StaticTenantContext(tenantId: null), domainVerificationService: new FakeDomainVerificationService(result: true));
        var sender = provider.GetRequiredService<ISender>();

        var tenantAId = await ProvisionAsync(sender);
        var tenantBId = await ProvisionAsync(sender);

        var registeredForA = await sender.Send(new RegisterTenantDomainCommand(tenantAId, domainName));
        registeredForA.IsSuccess.Should().BeTrue();
        var verifiedForA = await sender.Send(new VerifyTenantDomainCommand(registeredForA.Value!.Domain.Id));
        verifiedForA.IsSuccess.Should().BeTrue();

        var registeredForB = await sender.Send(new RegisterTenantDomainCommand(tenantBId, domainName));
        registeredForB.IsSuccess.Should().BeFalse();
        registeredForB.Code.Should().Be(ApiErrorCodes.TenantDomainAlreadyRegistered);

        var resolved = await sender.Send(new GetPublicBrandingQuery(domainName));
        resolved.IsSuccess.Should().BeTrue();
        resolved.Value!.Tenant.Id.Should().Be(tenantAId);

        var listForB = await sender.Send(new ListTenantDomainsQuery(tenantBId));
        listForB.IsSuccess.Should().BeTrue();
        listForB.Value!.Data.Should().BeEmpty();

        var listAll = await sender.Send(new ListTenantDomainsQuery(TenantId: null));
        listAll.IsSuccess.Should().BeTrue();
        listAll.Value!.Data.Should().ContainSingle(d => d.Domain == domainName && d.TenantId == tenantAId);
    }

    /// <summary>Cenário: "Falha de renovação" — a plataforma deve ser alertada.</summary>
    [Fact]
    public async Task Falha_Na_Renovacao_De_Certificado_Marca_Falho_E_Alerta_A_Plataforma()
    {
        var domainName = UniqueDomain();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        await using var setupProvider = MediatRTestContainerFactory.Build(
            db, new StaticTenantContext(tenantId: null), domainVerificationService: new FakeDomainVerificationService(result: true));
        var setupSender = setupProvider.GetRequiredService<ISender>();

        var tenantId = await ProvisionAsync(setupSender);
        var registered = await setupSender.Send(new RegisterTenantDomainCommand(tenantId, domainName));
        var domainId = registered.Value!.Domain.Id;
        var verified = await setupSender.Send(new VerifyTenantDomainCommand(domainId));
        verified.IsSuccess.Should().BeTrue(); // certificado emitido normalmente pelo ManualCertificateIssuer.

        await using var renewalDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId: null));
        var notifier = new RecordingPlatformAlertNotifier();
        await using var renewalProvider = MediatRTestContainerFactory.Build(
            renewalDb,
            new StaticTenantContext(tenantId: null),
            certificateIssuer: new FakeCertificateIssuer(),
            platformAlertNotifier: notifier);
        var renewalSender = renewalProvider.GetRequiredService<ISender>();

        var renewed = await renewalSender.Send(new RenewTenantDomainCertificateCommand(tenantId, domainId));

        renewed.IsSuccess.Should().BeFalse();
        renewed.Code.Should().Be(ApiErrorCodes.TenantDomainCertificateIssuanceFailed);
        notifier.CertificateRenewalFailedCalls.Should().ContainSingle(
            c => c.TenantId == tenantId && c.DomainId == domainId && c.Domain == domainName);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var domain = await readDb.TenantDomains.SingleAsync(d => d.Id == domainId);
        domain.CertStatus.Should().Be(Nexora.Domain.Platform.TenantDomainCertStatus.Failed);
    }

    private static async Task<Guid> ProvisionAsync(ISender sender)
    {
        var result = await sender.Send(new ProvisionTenantCommand(
            Name: "Pizzaria Dona Betinha",
            Slug: $"tenant-{Guid.NewGuid():N}",
            Plan: "COMPLETO",
            Template: "PIZZERIA",
            OwnerName: "Dona Betinha",
            OwnerEmail: $"owner-{Guid.NewGuid():N}@example.com",
            StoreName: "Matriz",
            StoreTimezone: "America/Sao_Paulo"));

        result.IsSuccess.Should().BeTrue();
        return result.Value!.Tenant.Id;
    }

    private static string UniqueDomain() => $"cardapio-{Guid.NewGuid():N}.donabetinha.com.br";
}
