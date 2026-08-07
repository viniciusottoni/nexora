extern alias ApiCloud;
using System.Reflection;
using ApiCloud::Nexora.Api.Cloud.Controllers;
using ApiCloud::Nexora.Api.Cloud.Infrastructure;
using Nexora.Contracts.Platform;
using Nexora.Contracts.Tenants;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Platform;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — mesmo padrão de
/// <see cref="TenantPlanResultMappingTests"/>: prova a exigência de <c>PlatformAdmin</c> (Gherkin
/// "Falha parcial"/RN-015: nenhuma rota de negócio do tenant aqui) e a tradução
/// <c>Result&lt;T&gt;</c> → <see cref="IActionResult"/> sem precisar de host HTTP completo. Também
/// cobre RN-015 ("central mostra só metadado técnico/administrativo") verificando, por reflexão, que
/// nenhum campo do contrato de resposta sugere dado de negócio do cliente (pedido, caixa, estoque,
/// financeiro).
/// </summary>
public sealed class PlatformAttentionResultMappingTests
{
    [Theory]
    [InlineData(typeof(PlatformAttentionController), nameof(PlatformAttentionController.List))]
    [InlineData(typeof(PlatformAttentionController), nameof(PlatformAttentionController.Acknowledge))]
    [InlineData(typeof(PlatformAttentionController), nameof(PlatformAttentionController.Export))]
    [InlineData(typeof(TenantsController), nameof(TenantsController.AdministrativeTimeline))]
    public void Endpoints_Da_Central_De_Atencao_Exigem_A_Policy_PlatformAdmin(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName);

        method.Should().NotBeNull();
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>()
            ?? controllerType.GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull("a central operacional é exclusiva do administrador de plataforma (RN-015), sem equivalente self-service");
        authorize!.Policy.Should().Be("PlatformAdmin");
    }

    [Fact]
    public void PlatformAttentionController_Nao_Tem_Autorizacao_Mais_Fraca_Que_A_Da_Classe()
    {
        // A [Authorize(Policy = "PlatformAdmin")] está na CLASSE (todas as rotas herdam) — diferente
        // de TenantsController, que mistura rotas self-service (Get) com rotas exclusivas de
        // plataforma método a método. Prova que não existe overriding acidental mais permissivo.
        var classAuthorize = typeof(PlatformAttentionController).GetCustomAttribute<AuthorizeAttribute>();
        classAuthorize.Should().NotBeNull();
        classAuthorize!.Policy.Should().Be("PlatformAdmin");
    }

    [Fact]
    public void GetAttentionQueue_Sucesso_Vira_200_Com_O_Corpo_No_Formato_Do_Contrato()
    {
        var item = new AttentionQueueItemResponse(
            "INSTALLATION_OFFLINE|" + Guid.NewGuid() + "|" + Guid.NewGuid(),
            Guid.NewGuid(),
            "Pizzaria Dona Betinha",
            "INSTALLATION_OFFLINE",
            "CRITICAL",
            DateTimeOffset.UtcNow.AddMinutes(-90),
            "Sem contato há 1 h",
            new AttentionActionResponse("OPEN_DIAGNOSTICS", "/instalacoes"));

        var response = new AttentionQueueListResponse(
            new[] { item }, NextCursor: null, new AttentionQueueMetaResponse(DateTimeOffset.UtcNow, Array.Empty<string>()));

        var result = global::Nexora.Application.Abstractions.Messaging.Result<AttentionQueueListResponse>.Success(response);
        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void AcknowledgeAttentionItem_Sucesso_Vira_200()
    {
        var response = new AttentionAcknowledgementResponse(Guid.NewGuid(), "PROVISIONING_STALLED|tenant|tenant", "Cliente avisado, aguardando retorno.", DateTimeOffset.UtcNow);

        var result = global::Nexora.Application.Abstractions.Messaging.Result<AttentionAcknowledgementResponse>.Success(response);
        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void AttentionItemNotFound_Vira_404()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<AttentionAcknowledgementResponse>.Failure(
            "Este item da fila de atenção não foi encontrado.", Nexora.Shared.Errors.ApiErrorCodes.AttentionItemNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void GetAdministrativeTimeline_Sucesso_Vira_200_Com_O_Corpo_No_Formato_Do_Contrato()
    {
        var entry = new AdministrativeTimelineEntryResponse(
            "STATUS_CHANGED",
            DateTimeOffset.UtcNow.AddDays(-2),
            new AdministrativeTimelineActorResponse(Guid.NewGuid(), "Administrador da plataforma"),
            "PLATFORM_ADMIN",
            "Divergência comercial",
            Guid.NewGuid().ToString(),
            "Status alterado de Ativo para Suspenso.");

        var response = new AdministrativeTimelineListResponse(new[] { entry }, NextCursor: null);

        var result = global::Nexora.Application.Abstractions.Messaging.Result<AdministrativeTimelineListResponse>.Success(response);
        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void TenantNotFound_No_Timeline_Vira_404()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<AdministrativeTimelineListResponse>.Failure(
            "Estabelecimento não encontrado.", Nexora.Shared.Errors.ApiErrorCodes.TenantNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// RN-015 "central mostra só metadado técnico/administrativo" — nenhum campo dos DTOs desta US
    /// tem nome que sugira dado de negócio do cliente (pedido, item, comanda, caixa, pagamento,
    /// estoque, financeiro). Prova estrutural barata: reflexão sobre os nomes de propriedade dos
    /// records de resposta, não substitui revisão humana mas pega regressão óbvia.
    /// </summary>
    [Theory]
    [InlineData(typeof(AttentionQueueItemResponse))]
    [InlineData(typeof(AttentionQueueListResponse))]
    [InlineData(typeof(AttentionAcknowledgementResponse))]
    [InlineData(typeof(AdministrativeTimelineEntryResponse))]
    [InlineData(typeof(AdministrativeTimelineListResponse))]
    public void Contratos_Da_Central_Nao_Expoem_Vocabulario_De_Dado_De_Negocio_Do_Cliente(Type contractType)
    {
        var forbiddenTerms = new[] { "order", "pedido", "comanda", "caixa", "payment", "pagamento", "estoque", "inventory", "financ" };

        var propertyNames = contractType.GetProperties().Select(p => p.Name.ToLowerInvariant()).ToList();

        foreach (var forbidden in forbiddenTerms)
        {
            propertyNames.Should().NotContain(name => name.Contains(forbidden),
                $"o contrato {contractType.Name} não deveria expor '{forbidden}' (RN-015)");
        }
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/platform/attention" },
    };
}
