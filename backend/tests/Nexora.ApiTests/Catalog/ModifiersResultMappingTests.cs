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
/// Contrato de propagação de erro (ADR-021) para os códigos do módulo de grupos de modificadores
/// (US-012, <c>Nexora.Shared.Errors.ApiErrorCodes.Modifiers</c>), já integrados em
/// <c>ResultExtensions.MapErrorCode</c> (<c>Nexora.Api.Cloud.Infrastructure</c>).
/// </summary>
public sealed class ModifiersResultMappingTests
{
    public static TheoryData<string, int> ModifierErrorCodes => new()
    {
        { ApiErrorCodes.ModifierGroupNotFound, StatusCodes.Status404NotFound },
        { ApiErrorCodes.ModifierNotFound, StatusCodes.Status404NotFound },
        { ApiErrorCodes.ModifierGroupProductNotFound, StatusCodes.Status404NotFound },
        { ApiErrorCodes.ModifierIngredientNotFound, StatusCodes.Status404NotFound },
        { ApiErrorCodes.ProductModifierGroupAlreadyLinked, StatusCodes.Status409Conflict },
        { ApiErrorCodes.ProductModifierGroupNotLinked, StatusCodes.Status404NotFound },
    };

    /// <summary>
    /// Independente do status HTTP mapeado, o corpo do <see cref="ProblemDetails"/> sempre carrega
    /// o código estável em <c>Extensions["code"]</c> — é esse campo que o frontend usa para decidir
    /// mensagem/telemetria (packages/contracts/src/errors.ts), não o texto de <c>title</c>/<c>detail</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(ModifierErrorCodes))]
    public void Falha_Com_Codigo_De_Modificador_Propaga_O_Codigo_No_ProblemDetails(string code, int expectedStatus)
    {
        _ = expectedStatus;
        var result = Result<string>.Failure("mensagem de teste em português", code);

        var actionResult = result.ToActionResult(new DefaultHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(code);
    }

    /// <summary>
    /// Cada código do módulo de modificadores está mapeado em <c>ResultExtensions.MapErrorCode</c>
    /// para o status HTTP correto — nenhum cai no catch-all 500 (ADR-021).
    /// </summary>
    [Theory]
    [MemberData(nameof(ModifierErrorCodes))]
    public void Falha_Com_Codigo_De_Modificador_Mapeia_Para_O_Status_Esperado(string code, int expectedStatus)
    {
        var result = Result<string>.Failure("mensagem de teste em português", code);

        var actionResult = result.ToActionResult(new DefaultHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatus);
    }

    /// <summary>
    /// Permissão negada (checada dentro dos handlers deste módulo, ver relatório da tarefa) já usa
    /// um código PRÉ-EXISTENTE e já mapeado (<c>AUTH_PERMISSION_DENIED</c>) — este caminho não
    /// depende de nenhuma integração futura em <c>MapErrorCode</c>.
    /// </summary>
    [Fact]
    public void Falha_De_Permissao_Ja_Mapeia_Para_403_Sem_Integracao_Adicional()
    {
        var result = Result<string>.Failure("Seu perfil não tem permissão para alterar o cardápio.", ApiErrorCodes.AuthPermissionDenied);

        var actionResult = result.ToActionResult(new DefaultHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }
}
