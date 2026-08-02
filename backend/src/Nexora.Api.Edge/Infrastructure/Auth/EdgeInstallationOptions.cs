namespace Nexora.Api.Edge.Infrastructure.Auth;

/// <summary>Configuração fixa da instalação edge — porta de EDGE_TENANT_ID (env var, apps/api-edge/src/modules/auth/auth.module.ts).</summary>
public sealed class EdgeInstallationOptions
{
    public const string SectionName = "Edge:Installation";

    public Guid? TenantId { get; set; }
}
