using Nexora.Application.Tenants.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Tenants;

/// <summary>
/// US-151 §12 "Unitário: ... cursor" — <see cref="TenantDirectoryCursor"/> é o keyset opaco de
/// <c>GET /v1/platform/tenants</c> (mesmo padrão de <c>AuditLogCursor</c>, ver
/// <c>AuditLogCursorTests</c> se existir), generalizado para os quatro critérios de
/// <see cref="TenantDirectorySort"/>.
/// </summary>
public sealed class TenantDirectoryCursorTests
{
    [Fact]
    public void Encode_Depois_Decode_Preserva_Primary_E_Id_Para_O_Mesmo_Sort()
    {
        var id = Guid.NewGuid();

        var cursor = TenantDirectoryCursor.Encode(TenantDirectorySort.Name, "Dona Betinha", id);
        var decoded = TenantDirectoryCursor.Decode(cursor, TenantDirectorySort.Name);

        decoded.Should().NotBeNull();
        decoded!.Primary.Should().Be("Dona Betinha");
        decoded.Id.Should().Be(id);
    }

    [Fact]
    public void Decode_Cursor_Nulo_Ou_Vazio_E_Tratado_Como_Primeira_Pagina()
    {
        TenantDirectoryCursor.Decode(null, TenantDirectorySort.Attention).Should().BeNull();
        TenantDirectoryCursor.Decode(string.Empty, TenantDirectorySort.Attention).Should().BeNull();
        TenantDirectoryCursor.Decode("   ", TenantDirectorySort.Attention).Should().BeNull();
    }

    [Fact]
    public void Decode_Cursor_Malformado_Retorna_Nulo_Em_Vez_De_Lancar()
    {
        TenantDirectoryCursor.Decode("isto-nao-e-base64-valido-!!!", TenantDirectorySort.Attention).Should().BeNull();
        TenantDirectoryCursor.Decode(Convert.ToBase64String("lixo-sem-separador"u8.ToArray()), TenantDirectorySort.Attention).Should().BeNull();
    }

    [Fact]
    public void Decode_Com_Sort_Diferente_Do_Que_Gerou_O_Cursor_Retorna_Nulo()
    {
        // Cliente trocou de ordenação no meio da navegação — cursor da página anterior não é mais
        // válido; tratar como primeira página é mais seguro do que decodificar um valor primário
        // que pertence a outro critério de ordenação.
        var cursor = TenantDirectoryCursor.Encode(TenantDirectorySort.Attention, "0:638000000000000000", Guid.NewGuid());

        TenantDirectoryCursor.Decode(cursor, TenantDirectorySort.Name).Should().BeNull();
    }

    [Fact]
    public void Cursor_Preserva_Nome_Com_Caracteres_Livres_Incluindo_Pipe()
    {
        var id = Guid.NewGuid();
        var cursor = TenantDirectoryCursor.Encode(TenantDirectorySort.Name, "Bar | Grill", id);

        var decoded = TenantDirectoryCursor.Decode(cursor, TenantDirectorySort.Name);

        decoded!.Primary.Should().Be("Bar | Grill");
    }
}
