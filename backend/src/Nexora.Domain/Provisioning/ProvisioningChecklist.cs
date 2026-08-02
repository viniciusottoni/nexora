namespace Nexora.Domain.Provisioning;

/// <summary>Item do checklist de ativação de um tenant recém-provisionado, exibido ao dono.</summary>
public sealed record ProvisioningChecklistItem(string Code, string Label, string Status);

/// <summary>
/// Constrói o checklist inicial de ativação (porta de <c>packages/domain/src/provisioning/checklist.ts</c>).
/// O primeiro passo já nasce concluído porque a própria criação do tenant é o passo "INSTANCE".
/// </summary>
public static class ProvisioningChecklist
{
    private static readonly (string Code, string Label)[] Steps =
    {
        ("INSTANCE", "Criar instância e domínio"),
        ("BRANDING", "Aplicar identidade visual"),
        ("CATALOG", "Carregar cardápio e fichas técnicas"),
        ("OPERATION", "Configurar mesas, perfis e regras"),
        ("EDGE", "Instalar servidor local e rede"),
        ("PAYMENTS", "Configurar meios de pagamento"),
        ("TRAINING", "Treinar equipe"),
        ("PILOT", "Executar piloto acompanhado"),
        ("ACTIVATION", "Ativar estabelecimento")
    };

    public static IReadOnlyList<ProvisioningChecklistItem> Create()
    {
        var items = new List<ProvisioningChecklistItem>(Steps.Length);
        for (var i = 0; i < Steps.Length; i++)
        {
            items.Add(new ProvisioningChecklistItem(
                Steps[i].Code,
                Steps[i].Label,
                i == 0 ? "COMPLETED" : "PENDING"));
        }

        return items;
    }
}
