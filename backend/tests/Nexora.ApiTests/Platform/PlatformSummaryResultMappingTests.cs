extern alias ApiCloud;
using System.Reflection;
using ApiCloud::Nexora.Api.Cloud.Controllers;
using Nexora.Contracts.Platform;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Platform;

/// <summary>US-150 — policy e contrato HTTP do resumo exibido na raiz do web-platform.</summary>
public sealed class PlatformSummaryResultMappingTests
{
    [Fact]
    public void Controller_Exige_A_Policy_PlatformAdmin()
    {
        var authorize = typeof(PlatformSummaryController).GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull();
        authorize!.Policy.Should().Be("PlatformAdmin");
    }

    [Fact]
    public void Metodo_Get_Declara_Resposta_200_Com_O_Contrato_Do_Resumo()
    {
        var method = typeof(PlatformSummaryController).GetMethod(nameof(PlatformSummaryController.Get));

        method.Should().NotBeNull();
        var response = method!.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Single(attribute => attribute.StatusCode == StatusCodes.Status200OK);

        response.Type.Should().Be<PlatformSummaryResponse>();
    }
}
