using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-153 §12 "Unitário: matriz completa de transições válidas e inválidas" — cobre
/// <see cref="Tenant.TransitionStatus"/> isolado de banco/HTTP (a validação da matriz em si é
/// coberta por <see cref="TenantStatusTransitionsTests"/>).
/// </summary>
public sealed class TenantTests
{
    [Fact]
    public void Create_Nasce_Provisioned_Com_StatusVersion_1()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");

        tenant.Status.Should().Be(TenantStatus.Provisioned);
        tenant.StatusVersion.Should().Be(1);
        tenant.ActivatedAt.Should().BeNull();
    }

    [Fact]
    public void TransitionStatus_Valida_Incrementa_Versao_E_Atualiza_UpdatedAt()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");
        var at = DateTimeOffset.UtcNow.AddMinutes(5);

        var previous = tenant.TransitionStatus(TenantStatus.Installing, at);

        previous.Should().Be(TenantStatus.Provisioned);
        tenant.Status.Should().Be(TenantStatus.Installing);
        tenant.StatusVersion.Should().Be(2);
        tenant.UpdatedAt.Should().Be(at);
    }

    [Fact]
    public void TransitionStatus_Invalida_Lanca_DomainException_E_Nao_Muda_Nada()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");

        var act = () => tenant.TransitionStatus(TenantStatus.Active, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
        tenant.Status.Should().Be(TenantStatus.Provisioned);
        tenant.StatusVersion.Should().Be(1);
    }

    [Fact]
    public void TransitionStatus_Para_Active_Marca_ActivatedAt_Apenas_Na_Primeira_Vez()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");
        var firstActivation = DateTimeOffset.UtcNow;

        tenant.TransitionStatus(TenantStatus.Installing, firstActivation);
        tenant.TransitionStatus(TenantStatus.Active, firstActivation);
        tenant.TransitionStatus(TenantStatus.Suspended, firstActivation.AddDays(10));

        var reactivatedAt = firstActivation.AddDays(20);
        tenant.TransitionStatus(TenantStatus.Active, reactivatedAt);

        tenant.ActivatedAt.Should().Be(firstActivation, "reativação não deve sobrescrever a primeira ativação");
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.StatusVersion.Should().Be(5);
    }

    [Fact]
    public void Cancelled_E_Terminal_TransitionStatus_Sempre_Lanca()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");
        tenant.TransitionStatus(TenantStatus.Cancelled, DateTimeOffset.UtcNow);

        var act = () => tenant.TransitionStatus(TenantStatus.Active, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }
}
