extern alias ApiCloud;
using ApiCloud::Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Catalog;

/// <summary>
/// US-144 (Importação de cardápio por planilha). Ao contrário do resto do catálogo
/// (<see cref="CatalogResultMappingTests"/>), <c>CatalogImportController</c> NÃO devolve os dois
/// códigos abaixo através do canal genérico <c>Result.ToActionResult</c> na prática — ambos são
/// interceptados no próprio controller antes disso (ver docstring de
/// <c>CatalogImportController.BuildInvalidFileProblem</c> e de <c>ImportCatalogCommandHandler</c>).
/// Esta suíte, portanto, não prova o comportamento HTTP real desses dois fluxos (isso é
/// <c>CatalogImportIntegrationTests</c>/E2E) — ela só documenta que o switch genérico
/// <c>ResultExtensions.MapErrorCode</c> (E-14, tarefa de integração final) agora tem entrada para
/// os dois códigos de <c>ApiErrorCodes.CatalogImport.cs</c>, para o dia em que alguém chamar
/// <c>Result.ToActionResult</c> diretamente com eles (ex.: se o fluxo migrar de vez para o canal
/// genérico de <c>Result</c>).
/// </summary>
public sealed class CatalogImportResultMappingTests
{
    [Fact]
    public void Catalog_Import_Invalid_File_Vira_400_Recuperavel()
    {
        var result = Result.Failure("Não foi possível ler o arquivo enviado.", ApiErrorCodes.CatalogImportInvalidFile);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Catalog_Import_Validation_Failed_Vira_422_Recuperavel()
    {
        var result = Result.Failure("Planilha contém linhas inválidas.", ApiErrorCodes.CatalogImportValidationFailed);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/catalog/import" },
    };
}
