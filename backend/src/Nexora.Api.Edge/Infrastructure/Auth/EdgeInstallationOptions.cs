namespace Nexora.Api.Edge.Infrastructure.Auth;

/// <summary>Configuração fixa da instalação edge — tenant e loja da instalação local.</summary>
public sealed class EdgeInstallationOptions
{
    public const string SectionName = "Edge:Installation";

    public Guid? TenantId { get; set; }

    /// <summary>
    /// Loja fixa do edge. Também é usada antes do login, no pareamento anônimo do primeiro
    /// dispositivo, quando ainda não existe uma claim <c>sid</c>.
    /// </summary>
    public Guid? StoreId { get; set; }
}
