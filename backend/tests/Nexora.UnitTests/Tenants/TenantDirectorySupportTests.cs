using Nexora.Application.Installations.Support;
using Nexora.Application.Tenants.Support;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Tenants;

/// <summary>
/// US-151 §12 "Unitário: serialização de filtros, ordenação e cursor" — normalização de status em
/// caixa alta (<see cref="TenantStatusWireFormat"/>), rótulos de <see cref="TenantDirectorySort"/>
/// (usados em <c>appliedFilters.sort</c>) e a agregação de saúde por tenant
/// (<see cref="TenantHealthClassifier"/>, incluindo o caso UNKNOWN — "nunca reportar DOWN por
/// engano" quando não há nenhuma instalação instalada).
/// </summary>
public sealed class TenantDirectorySupportTests
{
    // --- TenantStatusWireFormat ---

    [Theory]
    [InlineData(TenantStatus.Provisioned, "PROVISIONED")]
    [InlineData(TenantStatus.Installing, "INSTALLING")]
    [InlineData(TenantStatus.Active, "ACTIVE")]
    [InlineData(TenantStatus.Suspended, "SUSPENDED")]
    [InlineData(TenantStatus.Cancelled, "CANCELLED")]
    public void ToWireLabel_Nunca_Vaza_O_Enum_C_Cru(TenantStatus status, string expected)
    {
        status.ToWireLabel().Should().Be(expected);
    }

    // --- TenantDirectorySort ---

    [Theory]
    [InlineData(TenantDirectorySort.Name, "name")]
    [InlineData(TenantDirectorySort.CreatedAt, "createdAt")]
    [InlineData(TenantDirectorySort.UpdatedAt, "updatedAt")]
    [InlineData(TenantDirectorySort.Attention, "attention")]
    public void ToWireLabel_Usa_A_Mesma_Grafia_Do_Parametro_De_Entrada(TenantDirectorySort sort, string expected)
    {
        // Diferente de status/health (caixa alta), sort ecoa em appliedFilters exatamente como
        // chega na query string (US-151 §7, exemplo "sort": "attention" em minúsculas).
        sort.ToWireLabel().Should().Be(expected);
    }

    // --- TenantHealthClassifier ---

    [Fact]
    public void Classify_Sem_Nenhuma_Instalacao_Instalada_E_Unknown_Nunca_Down()
    {
        var now = DateTimeOffset.UtcNow;

        TenantHealthClassifier.Classify(now, Array.Empty<DateTimeOffset?>()).Should().Be(TenantHealthStatus.Unknown);
    }

    [Fact]
    public void Classify_Todas_As_Instalacoes_Saudaveis_E_Ok()
    {
        var now = DateTimeOffset.UtcNow;
        var values = new DateTimeOffset?[] { now, now.AddMinutes(-1) };

        TenantHealthClassifier.Classify(now, values).Should().Be(TenantHealthStatus.Ok);
    }

    [Fact]
    public void Classify_Usa_A_Pior_Instalacao_Entre_Varias()
    {
        var now = DateTimeOffset.UtcNow;
        var values = new DateTimeOffset?[]
        {
            now, // OK
            now.AddMinutes(-10), // DEGRADED
            null // sem contato — DOWN
        };

        TenantHealthClassifier.Classify(now, values).Should().Be(TenantHealthStatus.Down);
    }

    [Fact]
    public void Classify_Degraded_Prevalece_Sobre_Ok_Quando_Nao_Ha_Down()
    {
        var now = DateTimeOffset.UtcNow;
        var values = new DateTimeOffset?[] { now, now.AddMinutes(-10) };

        TenantHealthClassifier.Classify(now, values).Should().Be(TenantHealthStatus.Degraded);
    }

    [Theory]
    [InlineData(TenantHealthStatus.Ok, "OK")]
    [InlineData(TenantHealthStatus.Degraded, "DEGRADED")]
    [InlineData(TenantHealthStatus.Down, "DOWN")]
    [InlineData(TenantHealthStatus.Unknown, "UNKNOWN")]
    public void ToWireLabel_De_Saude_Bate_Com_O_Contrato(TenantHealthStatus status, string expected)
    {
        status.ToWireLabel().Should().Be(expected);
    }

    [Fact]
    public void InstallationHealthClassifier_Continua_Reusado_Sem_Alteracao_De_Comportamento()
    {
        // Blindagem contra regressão: TenantHealthClassifier PRECISA reusar o mesmo classificador
        // de instalação individual (US-151 instrução explícita "reuse exatamente este
        // classificador"), não reimplementar os limiares 5/15 min por conta própria.
        var now = DateTimeOffset.UtcNow;

        InstallationHealthClassifier.Classify(now, now.AddMinutes(-6)).Should().Be(InstallationHealthStatus.Degraded);
        TenantHealthClassifier.Classify(now, new DateTimeOffset?[] { now.AddMinutes(-6) }).Should().Be(TenantHealthStatus.Degraded);
    }
}
