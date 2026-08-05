using Nexora.Domain.Cashier;
using Nexora.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Nexora.UnitTests.Cashier;

/// <summary>
/// US-055/US-056 + docs/domain/04-Caixa-e-Pagamento.md: os invariantes críticos de caixa também
/// precisam existir no schema, não só nas entidades/validators.
/// </summary>
public sealed class CashierSchemaConfigurationTests
{
    [Fact]
    public void Cashier_Model_Configura_Checks_Normativos_De_Sessao_E_Movimento()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=nexora_schema_metadata")
            .Options);

        var model = db.GetService<IDesignTimeModel>().Model;

        var cashSessionChecks = model.FindEntityType(typeof(CashSession))!
            .GetCheckConstraints()
            .Select(c => c.Name)
            .ToList();
        var cashMovementChecks = model.FindEntityType(typeof(CashMovement))!
            .GetCheckConstraints()
            .Select(c => c.Name)
            .ToList();

        cashSessionChecks.Should().Contain("ck_cash_opening");
        cashSessionChecks.Should().Contain("ck_cash_closed");
        cashMovementChecks.Should().Contain("ck_movement_amount");
    }
}
