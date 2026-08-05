using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Provisioning;

/// <summary>
/// US-142 §4, cenário "Modelo sem código específico": "não deve haver condicional por tipo de
/// negócio" no caminho vivo do provisionamento. <c>infra/scripts/governance.ts</c> (a trava de CI
/// citada pelo CLAUDE.md) hoje só varre <c>apps/</c>/<c>packages/</c> (TS/JS/CSS) e um recorte
/// estreito de C# (comparação de tenant_id/cor literal) — não cobre este caso específico (switch/if
/// sobre o CÓDIGO do modelo de negócio). Este teste fecha essa lacuna para os arquivos que
/// substituíram o antigo <c>ProvisioningTemplates</c> (switch em código): varre o texto-fonte por
/// <c>case "PIZZERIA"</c>/<c>== "HAMBURGUERIA"</c> e primos — se algum aparecer de volta no caminho
/// vivo, é a regressão exata que esta US existe para impedir.
/// </summary>
public sealed class ProvisioningHasNoBusinessTypeConditionalTests
{
    private static readonly string[] BusinessTypeCodes =
    {
        "PIZZERIA", "HAMBURGUERIA", "RESTAURANTE", "LANCHONETE", "BURGER",
    };

    /// <summary>
    /// Só os arquivos do CAMINHO VIVO — <see cref="BusinessTemplateSeedCatalog"/> (fonte dos seeds,
    /// não código de decisão em runtime) é isento de propósito: ele CONSTRÓI os 4 modelos, não
    /// decide entre eles a partir de um tenant. O extinto <c>ProvisioningTemplates</c>/
    /// <c>ProvisioningTemplate.cs</c> também é isento — deliberadamente mantido no repositório
    /// (fonte do conteúdo verbatim da pizzaria) mas fora do caminho vivo (ver relatório da tarefa).
    /// </summary>
    public static IEnumerable<object[]> LiveProvisioningFiles()
    {
        var root = RepositoryRoot();
        var files = new[]
        {
            "backend/src/Nexora.Application/Tenants/Commands/ProvisionTenant/ProvisionTenantCommandHandler.cs",
            "backend/src/Nexora.Application/Provisioning/BusinessTemplateDataMapper.cs",
            "backend/src/Nexora.Application/Provisioning/Queries/ListBusinessTemplates/ListBusinessTemplatesQueryHandler.cs",
            "backend/src/Nexora.Application/Provisioning/Queries/GetBusinessTemplate/GetBusinessTemplateQueryHandler.cs",
            "backend/src/Nexora.Application/Provisioning/Commands/UpdateBusinessTemplate/UpdateBusinessTemplateCommandHandler.cs",
            "backend/src/Nexora.Api.Cloud/Controllers/BusinessTemplatesController.cs",
        };

        foreach (var relativePath in files)
        {
            yield return new object[] { Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)) };
        }
    }

    [Theory]
    [MemberData(nameof(LiveProvisioningFiles))]
    public void Arquivo_Do_Caminho_Vivo_Nao_Contem_Condicional_Por_Codigo_De_Modelo_De_Negocio(string filePath)
    {
        File.Exists(filePath).Should().BeTrue($"o arquivo esperado não existe em {filePath}");
        var source = File.ReadAllText(filePath);

        foreach (var code in BusinessTypeCodes)
        {
            // Bloqueia switch/case, comparação de igualdade e o padrão de dicionário-por-código
            // (ex.: `case "PIZZERIA"`, `== "HAMBURGUERIA"`, `["RESTAURANTE"]`) — o mesmo espírito de
            // ADR-013, adaptado de "por tenant" para "por tipo de negócio" (o objeto desta US).
            var forbiddenPatterns = new[]
            {
                $@"case\s+""{code}""",
                $@"[=!]=\s*""{code}""",
                $@"""{code}""\s*[=!]=",
                $@"\[\s*""{code}""\s*\]",
            };

            foreach (var pattern in forbiddenPatterns)
            {
                Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase).Should().BeFalse(
                    $"{filePath} não deveria ter condicional/lookup fixo por \"{code}\" (padrão /{pattern}/) — ADR-013.");
            }
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CLAUDE.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Não foi possível localizar a raiz do repositório (CLAUDE.md).");
    }
}
