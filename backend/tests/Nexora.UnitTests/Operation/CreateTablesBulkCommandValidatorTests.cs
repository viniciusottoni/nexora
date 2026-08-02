using Nexora.Application.Tables.Commands.CreateTablesBulk;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>Cenário Gherkin "Criação em lote" — validação de intervalo antes de tocar o banco.</summary>
public sealed class CreateTablesBulkCommandValidatorTests
{
    private readonly CreateTablesBulkCommandValidator _sut = new();

    [Fact]
    public void Intervalo_Um_A_Vinte_E_Valido()
    {
        var result = _sut.Validate(new CreateTablesBulkCommand(Guid.NewGuid(), 1, 20, 4));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void From_Maior_Que_To_E_Invalido()
    {
        var result = _sut.Validate(new CreateTablesBulkCommand(Guid.NewGuid(), 20, 1, 4));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Lote_Maior_Que_O_Teto_E_Invalido()
    {
        var result = _sut.Validate(new CreateTablesBulkCommand(Guid.NewGuid(), 1, CreateTablesBulkCommandValidator.MaxBatchSize + 1, 4));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void From_Zero_Ou_Negativo_E_Invalido()
    {
        var result = _sut.Validate(new CreateTablesBulkCommand(Guid.NewGuid(), 0, 10, 4));

        result.IsValid.Should().BeFalse();
    }
}
