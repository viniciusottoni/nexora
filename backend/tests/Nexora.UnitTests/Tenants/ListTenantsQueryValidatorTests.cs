using Nexora.Application.Tenants.Queries.ListTenants;
using Nexora.Application.Tenants.Support;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Tenants;

/// <summary>US-151 §7 "limit — default 25, máximo 100" e §7 (intervalo de criação inclusive).</summary>
public sealed class ListTenantsQueryValidatorTests
{
    private static ListTenantsQuery BuildQuery(
        int limit = 25,
        DateTimeOffset? createdFrom = null,
        DateTimeOffset? createdTo = null) => new(
            SearchTerm: null,
            Statuses: Array.Empty<TenantStatus>(),
            Plans: Array.Empty<string>(),
            Templates: Array.Empty<string>(),
            HealthStatuses: Array.Empty<TenantHealthStatus>(),
            CreatedFrom: createdFrom,
            CreatedTo: createdTo,
            Sort: TenantDirectorySort.Attention,
            Limit: limit,
            Cursor: null);

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(100)]
    public void Limite_Dentro_Do_Intervalo_Permitido_E_Valido(int limit)
    {
        var result = new ListTenantsQueryValidator().Validate(BuildQuery(limit: limit));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Limite_Zero_E_Invalido()
    {
        var result = new ListTenantsQueryValidator().Validate(BuildQuery(limit: 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ListTenantsQuery.Limit));
    }

    [Fact]
    public void Limite_Acima_De_100_E_Invalido()
    {
        var result = new ListTenantsQueryValidator().Validate(BuildQuery(limit: 101));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ListTenantsQuery.Limit));
    }

    [Fact]
    public void CreatedFrom_Depois_De_CreatedTo_E_Invalido()
    {
        var now = DateTimeOffset.UtcNow;

        var result = new ListTenantsQueryValidator().Validate(
            BuildQuery(createdFrom: now, createdTo: now.AddDays(-1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreatedFrom_Igual_A_CreatedTo_E_Valido_Limite_Inclusivo()
    {
        var now = DateTimeOffset.UtcNow;

        var result = new ListTenantsQueryValidator().Validate(BuildQuery(createdFrom: now, createdTo: now));

        result.IsValid.Should().BeTrue();
    }
}
