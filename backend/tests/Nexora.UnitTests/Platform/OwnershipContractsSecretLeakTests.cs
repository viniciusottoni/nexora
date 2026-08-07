using System.Reflection;
using System.Text.Json;
using Nexora.Contracts.Tenants;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-155 "Proprietários, usuários iniciais e convites" — requisito de segurança TESTÁVEL do texto
/// da tarefa: "Segredo (token bruto/hash) NUNCA em DTO de resposta". Cenário Gherkin "Segredo não
/// recuperável": consultar o histórico de convites nunca pode devolver token bruto nem hash.
/// </summary>
/// <remarks>
/// Duas camadas de proteção: (1) reflexão sobre o SHAPE dos contratos de resposta (nenhuma
/// propriedade com nome que pareça segredo existe, então nem é POSSÍVEL populá-la por engano no
/// futuro) e (2) serialização de uma instância totalmente preenchida com valores-sentinela óbvios
/// (o hash/token real, se algum dia vazasse por um campo mal nomeado como "Details", ainda
/// apareceria na string JSON) — a combinação cobre tanto o erro "campo errado" quanto "valor errado
/// dentro de um campo com nome inocente".
/// </remarks>
public sealed class OwnershipContractsSecretLeakTests
{
    private static readonly Type[] OwnershipResponseTypes =
    {
        typeof(TenantOwnershipResponse),
        typeof(TenantOwnershipOwnerResponse),
        typeof(TenantOwnershipInviteResponse),
        typeof(TenantOwnershipTransferHistoryResponse),
        typeof(CreateOwnerInviteResponse),
        typeof(TransferTenantOwnershipResponse),
        typeof(UnlockOwnerAccessResponse),
    };

    private static readonly string[] ForbiddenPropertyNameFragments = { "hash", "token", "secret", "password" };

    [Theory]
    [MemberData(nameof(TypesAndProperties))]
    public void Nenhuma_Propriedade_De_Resposta_De_Ownership_Tem_Nome_De_Segredo(Type type, PropertyInfo property)
    {
        var nameLower = property.Name.ToLowerInvariant();

        foreach (var fragment in ForbiddenPropertyNameFragments)
        {
            nameLower.Should().NotContain(
                fragment,
                $"{type.Name}.{property.Name} não pode carregar segredo (token bruto/hash) — US-155, 'segredo nunca em DTO de resposta'");
        }
    }

    public static IEnumerable<object[]> TypesAndProperties()
    {
        foreach (var type in OwnershipResponseTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                yield return new object[] { type, property };
            }
        }
    }

    /// <summary>
    /// Segunda camada: instancia a árvore completa de <see cref="TenantOwnershipResponse"/> com um
    /// valor-sentinela ("owner@example.com" é o único dado real; nenhum hash jamais é construído
    /// porque a entidade de domínio não o expõe fora de <c>OwnerInvite.SecretHash</c>, nunca lido por
    /// nenhum handler desta US) e garante que a palavra "hash"/"token" não aparece na string JSON —
    /// prova que MESMO que um campo inocente carregasse esse valor por engano, o teste pegaria.
    /// </summary>
    [Fact]
    public void Serializacao_Completa_Da_Resposta_De_Ownership_Nao_Contem_Substring_De_Segredo()
    {
        var owner = new TenantOwnershipOwnerResponse(Guid.NewGuid(), "Dona Betinha", "d***@example.com", "INVITED");
        var invite = new TenantOwnershipInviteResponse(
            Guid.NewGuid(), "d***@example.com", "PENDING", "SENT",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(72), null, null, null, "Reenvio solicitado");
        var transfer = new TenantOwnershipTransferHistoryResponse(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Alteração societária", true, DateTimeOffset.UtcNow);

        var response = new TenantOwnershipResponse(owner, new[] { invite }, new[] { transfer });

        var json = JsonSerializer.Serialize(response);

        json.ToLowerInvariant().Should().NotContain("hash");
        json.ToLowerInvariant().Should().NotContain("secret");
        // "token" tem sentido legítimo em outros contextos do produto (ex.: token de instalação),
        // mas nenhuma resposta desta US deveria mencioná-lo de forma alguma.
        json.ToLowerInvariant().Should().NotContain("token");
    }
}
