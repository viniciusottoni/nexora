using Nexora.Application.Orders.Support;
using Nexora.Shared.Errors;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>
/// US-030 §4/§5/§12 ("Validação de todos os casos de grupo de modificadores") — cenário Gherkin
/// "Grupo de modificadores obrigatório pendente" e as regras de mínimo/máximo/opcional, isoladas de
/// qualquer banco (o handler resolve <see cref="ModifierGroupValidator.GroupSpec"/> a partir do
/// produto; aqui a entrada já vem pronta).
/// </summary>
public sealed class ModifierGroupValidatorTests
{
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid ModifierA = Guid.NewGuid();
    private static readonly Guid ModifierB = Guid.NewGuid();

    /// <summary>Cenário Gherkin "Grupo de modificadores obrigatório pendente" (US-030 §4).</summary>
    [Fact]
    public void Grupo_Obrigatorio_Sem_Nenhuma_Escolha_Retorna_Modifier_Group_Required()
    {
        var group = new ModifierGroupValidator.GroupSpec(GroupId, "Tamanho", MinSelect: 1, MaxSelect: 1, IsRequired: true, new[] { ModifierA, ModifierB });

        var violation = ModifierGroupValidator.ValidateGroup(group, Array.Empty<Guid>());

        violation.Should().NotBeNull();
        violation!.Code.Should().Be(ApiErrorCodes.ModifierGroupRequired);
        violation.GroupId.Should().Be(GroupId);
        violation.GroupName.Should().Be("Tamanho");
    }

    [Fact]
    public void Grupo_Obrigatorio_Com_Escolha_Dentro_Do_Intervalo_Passa()
    {
        var group = new ModifierGroupValidator.GroupSpec(GroupId, "Tamanho", MinSelect: 1, MaxSelect: 1, IsRequired: true, new[] { ModifierA, ModifierB });

        var violation = ModifierGroupValidator.ValidateGroup(group, new[] { ModifierA });

        violation.Should().BeNull();
    }

    [Fact]
    public void Grupo_Opcional_Sem_Escolha_Nao_E_Violacao()
    {
        var group = new ModifierGroupValidator.GroupSpec(GroupId, "Adicionais", MinSelect: 0, MaxSelect: 3, IsRequired: false, new[] { ModifierA, ModifierB });

        var violation = ModifierGroupValidator.ValidateGroup(group, Array.Empty<Guid>());

        violation.Should().BeNull("grupo opcional sem escolha nenhuma é um estado válido");
    }

    [Fact]
    public void Escolha_Abaixo_Do_Minimo_Retorna_Modifier_Group_Selection_Invalid()
    {
        var group = new ModifierGroupValidator.GroupSpec(GroupId, "Escolha 2 sabores", MinSelect: 2, MaxSelect: 2, IsRequired: true, new[] { ModifierA, ModifierB, Guid.NewGuid() });

        var violation = ModifierGroupValidator.ValidateGroup(group, new[] { ModifierA });

        violation.Should().NotBeNull();
        violation!.Code.Should().Be(ApiErrorCodes.ModifierGroupSelectionInvalid);
        violation.Selected.Should().Be(1);
        violation.MinSelect.Should().Be(2);
    }

    [Fact]
    public void Escolha_Acima_Do_Maximo_Retorna_Modifier_Group_Selection_Invalid()
    {
        var thirdModifier = Guid.NewGuid();
        var group = new ModifierGroupValidator.GroupSpec(GroupId, "Adicionais", MinSelect: 0, MaxSelect: 2, IsRequired: false, new[] { ModifierA, ModifierB, thirdModifier });

        var violation = ModifierGroupValidator.ValidateGroup(group, new[] { ModifierA, ModifierB, thirdModifier });

        violation.Should().NotBeNull();
        violation!.Code.Should().Be(ApiErrorCodes.ModifierGroupSelectionInvalid);
        violation.Selected.Should().Be(3);
        violation.MaxSelect.Should().Be(2);
    }

    [Fact]
    public void Modificador_De_Outro_Grupo_Nao_Conta_Para_A_Escolha_Deste_Grupo()
    {
        var group = new ModifierGroupValidator.GroupSpec(GroupId, "Tamanho", MinSelect: 1, MaxSelect: 1, IsRequired: true, new[] { ModifierA });
        var modifierDeOutroGrupo = Guid.NewGuid();

        var violation = ModifierGroupValidator.ValidateGroup(group, new[] { modifierDeOutroGrupo });

        violation.Should().NotBeNull("o modificador escolhido não pertence a este grupo, então a escolha continua vazia para ele");
        violation!.Code.Should().Be(ApiErrorCodes.ModifierGroupRequired);
    }

    [Fact]
    public void ValidateAll_Devolve_A_Primeira_Violacao_Entre_Varios_Grupos()
    {
        var groupOk = new ModifierGroupValidator.GroupSpec(Guid.NewGuid(), "Adicionais", 0, 3, false, new[] { ModifierA });
        var groupPendente = new ModifierGroupValidator.GroupSpec(GroupId, "Tamanho", 1, 1, true, new[] { ModifierB });

        var violation = ModifierGroupValidator.ValidateAll(new[] { groupOk, groupPendente }, Array.Empty<Guid>());

        violation.Should().NotBeNull();
        violation!.GroupId.Should().Be(GroupId);
    }

    [Fact]
    public void ValidateAll_Sem_Nenhum_Grupo_Vinculado_Ao_Produto_Nunca_Falha()
    {
        var violation = ModifierGroupValidator.ValidateAll(Array.Empty<ModifierGroupValidator.GroupSpec>(), new[] { ModifierA });

        violation.Should().BeNull("produto sem nenhum grupo de modificador configurado aceita qualquer seleção");
    }
}
