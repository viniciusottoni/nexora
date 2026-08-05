using Nexora.Domain.Platform;

namespace Nexora.Application.Tenants.Support;

/// <summary>
/// US-151 §7 "o contrato usa códigos de status normalizados em caixa alta; valores internos do
/// enum não podem vazar como Trial ou número" — mesma inconsistência já reconhecida em
/// <c>ProvisionTenantCommandHandler</c> (comentário próximo a "PROVISIONED"), fechada aqui como o
/// ponto único de tradução <see cref="TenantStatus"/> → wire, reusado por todo endpoint que
/// devolve o status do tenant nesta US.
/// </summary>
public static class TenantStatusWireFormat
{
    public static string ToWireLabel(this TenantStatus status) => status switch
    {
        TenantStatus.Provisioned => "PROVISIONED",
        TenantStatus.Installing => "INSTALLING",
        TenantStatus.Active => "ACTIVE",
        TenantStatus.Suspended => "SUSPENDED",
        TenantStatus.Cancelled => "CANCELLED",
        _ => status.ToString().ToUpperInvariant()
    };
}
