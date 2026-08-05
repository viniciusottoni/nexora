namespace Nexora.Application.Tenants.Support;

/// <summary>
/// Critério de ordenação do diretório de estabelecimentos (US-151 §7, parâmetro <c>sort</c>).
/// Vinculado por <c>[FromQuery]</c> diretamente pelo model binder padrão do ASP.NET Core
/// (<c>Enum.TryParse(ignoreCase: true)</c>), então os nomes dos membros já são a grafia aceita na
/// query string (<c>name</c>/<c>createdAt</c>/<c>updatedAt</c>/<c>attention</c>, comparação
/// insensível a caixa).
/// </summary>
public enum TenantDirectorySort
{
    Name,
    CreatedAt,
    UpdatedAt,
    Attention
}

public static class TenantDirectorySortExtensions
{
    /// <summary>Rótulo estável usado em <c>appliedFilters.sort</c> e como namespace do cursor — camelCase, igual ao parâmetro de entrada (diferente de status/health, que saem em caixa alta).</summary>
    public static string ToWireLabel(this TenantDirectorySort sort) => sort switch
    {
        TenantDirectorySort.Name => "name",
        TenantDirectorySort.CreatedAt => "createdAt",
        TenantDirectorySort.UpdatedAt => "updatedAt",
        TenantDirectorySort.Attention => "attention",
        _ => "attention"
    };
}
