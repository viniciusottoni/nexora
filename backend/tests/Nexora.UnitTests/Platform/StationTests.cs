using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-017 (Cadastro de praças de produção) §12 ("Unitário: validação de gargalo único e de
/// capacidade positiva") — cobre as invariantes de <see cref="Station"/> isoladas de banco/HTTP.
/// A exclusividade do gargalo ENTRE praças (só uma marcada por vez) é responsabilidade da
/// Application (ver <c>CreateStationCommandHandler</c>/<c>UpdateStationCommandHandler</c>,
/// cobertos em <c>Nexora.IntegrationTests.StationsIntegrationTests</c>) — aqui só a mecânica de
/// marcar/desmarcar UMA praça.
/// </summary>
public sealed class StationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid StoreId = Guid.NewGuid();

    [Fact]
    public void Create_Com_Codigo_Vazio_Lanca_DomainException()
    {
        var act = () => Station.Create(TenantId, StoreId, code: "", name: "Forno");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Com_Nome_Vazio_Lanca_DomainException()
    {
        var act = () => Station.Create(TenantId, StoreId, code: "OVEN", name: "   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Preenche_Campos_Padrao_E_Nao_Marca_Gargalo_Por_Padrao()
    {
        var station = Station.Create(TenantId, StoreId, code: "OVEN", name: "Forno");

        station.Code.Should().Be("OVEN");
        station.Name.Should().Be("Forno");
        station.IsBottleneck.Should().BeFalse();
        station.IsActive.Should().BeTrue();
        station.Color.Should().BeNull();
        station.CapacitySlots.Should().BeNull();
        station.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_Aceita_Cor_E_Marcacao_De_Gargalo_Explicitas()
    {
        var station = Station.Create(
            TenantId, StoreId, code: "OVEN", name: "Forno",
            type: StationType.Oven, sortOrder: 2, color: "#C1121F", isBottleneck: true);

        station.Color.Should().Be("#C1121F");
        station.IsBottleneck.Should().BeTrue();
        station.SortOrder.Should().Be(2);
        station.Type.Should().Be(StationType.Oven);
    }

    [Fact]
    public void MarkAsBottleneck_Marca_A_Praca_Como_Gargalo()
    {
        var station = Station.Create(TenantId, StoreId, code: "OVEN", name: "Forno");

        station.MarkAsBottleneck();

        station.IsBottleneck.Should().BeTrue();
    }

    [Fact]
    public void UnmarkAsBottleneck_Desmarca_A_Praca_Como_Gargalo()
    {
        var station = Station.Create(TenantId, StoreId, code: "OVEN", name: "Forno", isBottleneck: true);

        station.UnmarkAsBottleneck();

        station.IsBottleneck.Should().BeFalse();
    }

    [Fact]
    public void UpdateCapacity_Atualiza_Slots_E_Tempo_Medio_De_Preparo()
    {
        var station = Station.Create(TenantId, StoreId, code: "OVEN", name: "Forno");

        station.UpdateCapacity(capacitySlots: 5, avgCookSeconds: 420);

        station.CapacitySlots.Should().Be((short)5);
        station.AvgCookSeconds.Should().Be(420);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateCapacity_Com_Capacidade_Nao_Positiva_Lanca_DomainException(short capacitySlots)
    {
        var station = Station.Create(TenantId, StoreId, code: "OVEN", name: "Forno");

        var act = () => station.UpdateCapacity(capacitySlots, avgCookSeconds: 420);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateDetails_Com_Nome_Vazio_Lanca_DomainException()
    {
        var station = Station.Create(TenantId, StoreId, code: "OVEN", name: "Forno");

        var act = () => station.UpdateDetails(name: "  ", color: "#000000", sortOrder: 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateDetails_Atualiza_Nome_Cor_E_Posicao()
    {
        var station = Station.Create(TenantId, StoreId, code: "OVEN", name: "Forno");

        station.UpdateDetails(name: "Forno a lenha", color: "#123456", sortOrder: 3);

        station.Name.Should().Be("Forno a lenha");
        station.Color.Should().Be("#123456");
        station.SortOrder.Should().Be((short)3);
    }

    [Fact]
    public void SoftDelete_Marca_DeletedAt_Sem_Remover_Fisicamente()
    {
        var station = Station.Create(TenantId, StoreId, code: "OVEN", name: "Forno");

        station.SoftDelete();

        station.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_E_Activate_Alternam_IsActive()
    {
        var station = Station.Create(TenantId, StoreId, code: "OVEN", name: "Forno");

        station.Deactivate();
        station.IsActive.Should().BeFalse();

        station.Activate();
        station.IsActive.Should().BeTrue();
    }
}
