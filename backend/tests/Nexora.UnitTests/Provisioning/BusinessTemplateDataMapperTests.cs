using System.Text.Json;
using Nexora.Application.Provisioning;
using Nexora.Domain.Provisioning;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Provisioning;

/// <summary>
/// Prova o round-trip JSON ⇄ <see cref="ProvisioningTemplate"/> (US-142) para os 4 modelos —
/// exatamente o caminho que a migration <c>AddBusinessTemplateSeeds</c> grava e que
/// <c>ProvisionTenantCommandHandler</c> lê de volta em runtime. Uma quebra de compatibilidade neste
/// mapeador (ex.: renomear uma propriedade do DTO) corromperia silenciosamente todo provisionamento
/// novo sem este teste. Dicionários com valor <c>object?</c> comparam pela representação JSON (não
/// por igualdade de tipo): o original guarda <c>int</c>/<c>bool</c>/<c>string</c> nativos, o
/// deserializado guarda <see cref="JsonElement"/> — os dois serializam para o mesmo texto quando o
/// conteúdo é equivalente, que é o que importa para o handler (que só volta a serializar).
/// </summary>
public sealed class BusinessTemplateDataMapperTests
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    public static IEnumerable<object[]> AllTemplates() =>
        BusinessTemplateSeedCatalog.All().Select(t => new object[] { t.Code, t.Template });

    [Theory]
    [MemberData(nameof(AllTemplates))]
    public void Serialize_Depois_ToProvisioningTemplate_Preserva_Todo_O_Conteudo(string code, ProvisioningTemplate original)
    {
        var (configJson, seedsJson) = BusinessTemplateDataMapper.Serialize(original);
        var entity = BusinessTemplate.Create(code, code, configJson, seedsJson);

        var roundTripped = BusinessTemplateDataMapper.ToProvisioningTemplate(entity);

        AsJson(roundTripped.Config.Branding).Should().Be(AsJson(original.Config.Branding));
        AsJson(roundTripped.Config.Operation).Should().Be(AsJson(original.Config.Operation));
        AsJson(roundTripped.Config.Thresholds).Should().Be(AsJson(original.Config.Thresholds));
        AsJson(roundTripped.Config.Modules).Should().Be(AsJson(original.Config.Modules));

        roundTripped.Roles.Should().BeEquivalentTo(original.Roles);
        roundTripped.Stations.Should().BeEquivalentTo(original.Stations);
        roundTripped.ExpenseCategories.Should().BeEquivalentTo(original.ExpenseCategories);
        roundTripped.FinancialAccounts.Should().BeEquivalentTo(original.FinancialAccounts);
    }

    [Theory]
    [MemberData(nameof(AllTemplates))]
    public void ExtractBottleneckResource_Le_O_Recurso_Depois_Do_Round_Trip_Por_Json(string code, ProvisioningTemplate original)
    {
        var (configJson, _) = BusinessTemplateDataMapper.Serialize(original);
        var configDto = JsonSerializer.Deserialize<BusinessTemplateConfigDto>(configJson, CaseInsensitiveOptions)!;

        var resourceAfterRoundTrip = BusinessTemplateDataMapper.ExtractBottleneckResource(configDto.Operation!);
        var resourceFromOriginal = BusinessTemplateDataMapper.ExtractBottleneckResource(original.Config.Operation);

        resourceAfterRoundTrip.Should().Be(resourceFromOriginal);
        resourceAfterRoundTrip.Should().NotBeNullOrEmpty($"o modelo {code} precisa declarar sua praça-gargalo");
    }

    [Fact]
    public void ExtractBottleneckResource_Sem_Bottleneck_Retorna_Nulo()
    {
        var operation = new Dictionary<string, object?> { ["serviceFeePercent"] = 10 };

        BusinessTemplateDataMapper.ExtractBottleneckResource(operation).Should().BeNull();
    }

    private static string AsJson(object value) => JsonSerializer.Serialize(value);
}
