using Nexora.Shared.Security;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Auth;

/// <summary>
/// US-004, gap "RBAC não verifica permissão em nenhum endpoint de negócio" — <see cref="PermissionAuthorization.HasPermission"/>
/// é o predicado por trás de TODA policy de permissão granular registrada em <c>Program.cs</c>
/// (<c>DeviceManage</c>/<c>ConfigWrite</c> da US-003/US-005, e agora <c>RoleRead</c>/<c>RoleWrite</c>
/// desta correção — ver <c>RolesController</c>). Sem teste dedicado antes desta correção; cobre
/// aqui os três formatos de permissão do catálogo (<c>Nexora.Domain.Platform.PermissionCatalog</c>):
/// código exato, curinga por recurso (<c>"user:*"</c>) e curinga total (<c>"*"</c>).
/// </summary>
public sealed class PermissionAuthorizationTests
{
    [Fact]
    public void Codigo_Exato_Satisfaz_A_Permissao_Exigida()
    {
        PermissionAuthorization.HasPermission(new[] { "user:read" }, "user:read").Should().BeTrue();
    }

    [Fact]
    public void Codigo_De_Outro_Recurso_Nao_Satisfaz()
    {
        PermissionAuthorization.HasPermission(new[] { "device:manage" }, "user:write").Should().BeFalse();
    }

    [Fact]
    public void Curinga_Por_Recurso_Satisfaz_Qualquer_Acao_Do_Mesmo_Recurso()
    {
        PermissionAuthorization.HasPermission(new[] { "user:*" }, "user:read").Should().BeTrue();
        PermissionAuthorization.HasPermission(new[] { "user:*" }, "user:write").Should().BeTrue();
    }

    [Fact]
    public void Curinga_Por_Recurso_Nao_Satisfaz_Outro_Recurso()
    {
        PermissionAuthorization.HasPermission(new[] { "device:*" }, "user:write").Should().BeFalse();
    }

    [Fact]
    public void Curinga_Total_Satisfaz_Qualquer_Permissao()
    {
        PermissionAuthorization.HasPermission(new[] { "*" }, "user:write").Should().BeTrue();
        PermissionAuthorization.HasPermission(new[] { "*" }, "tenant:manage").Should().BeTrue();
    }

    [Fact]
    public void Lista_Vazia_Nunca_Satisfaz()
    {
        PermissionAuthorization.HasPermission(Array.Empty<string>(), "user:read").Should().BeFalse();
    }

    [Fact]
    public void Prefixo_Sem_O_Separador_De_Curinga_Nao_E_Tratado_Como_Curinga()
    {
        // "user" (sem ":*") não deveria satisfazer "user:read" — só "user:*" (com o separador) é
        // curinga de recurso; garante que a checagem de sufixo não seja mais permissiva do que o
        // pretendido.
        PermissionAuthorization.HasPermission(new[] { "user" }, "user:read").Should().BeFalse();
    }
}
