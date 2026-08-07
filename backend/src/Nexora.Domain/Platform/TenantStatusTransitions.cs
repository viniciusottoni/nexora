namespace Nexora.Domain.Platform;

/// <summary>
/// US-153 · Ciclo de vida do estabelecimento — matriz canônica de transições válidas entre
/// PROVISIONED, INSTALLING, ACTIVE, SUSPENDED, CANCELLED (doc §3.1/§4). Único ponto de verdade:
/// usado pelo Domain (<see cref="Tenant.TransitionStatus"/>) e pela Application (pré-checagem que
/// devolve 409 TENANT_STATUS_TRANSITION_INVALID em vez de deixar a <see cref="Nexora.Domain.Common.DomainException"/>
/// virar 500). CANCELLED é terminal — nenhuma transição sai dele (doc §4, cenário "Transição inválida").
/// </summary>
public static class TenantStatusTransitions
{
    private static readonly IReadOnlyDictionary<TenantStatus, TenantStatus[]> Allowed =
        new Dictionary<TenantStatus, TenantStatus[]>
        {
            [TenantStatus.Provisioned] = new[] { TenantStatus.Installing, TenantStatus.Cancelled },
            [TenantStatus.Installing] = new[] { TenantStatus.Active, TenantStatus.Cancelled },
            [TenantStatus.Active] = new[] { TenantStatus.Suspended, TenantStatus.Cancelled },
            [TenantStatus.Suspended] = new[] { TenantStatus.Active, TenantStatus.Cancelled },
            [TenantStatus.Cancelled] = Array.Empty<TenantStatus>(),
        };

    public static bool IsValid(TenantStatus from, TenantStatus to) =>
        from != to && Allowed.TryGetValue(from, out var targets) && Array.IndexOf(targets, to) >= 0;

    /// <summary>
    /// Alvos que o endpoint administrativo (<c>POST .../status-transitions</c>) aceita a partir de
    /// <paramref name="from"/> — o mesmo conjunto de <see cref="IsValid"/>, exceto
    /// <see cref="TenantStatus.Installing"/>: essa transição só acontece pelo registro técnico da
    /// instalação edge (<c>RegisterInstallationCommandHandler</c>), nunca por decisão direta do
    /// administrador. Também alimenta <c>TenantOverviewResponse.tenant.availableTransitions</c>
    /// (doc §10 "próxima transição permitida") — única fonte de verdade da máquina de estados;
    /// o frontend nunca reimplementa esta matriz.
    /// </summary>
    public static IReadOnlyList<TenantStatus> AdminTargetsFrom(TenantStatus from) =>
        Allowed.TryGetValue(from, out var targets)
            ? Array.FindAll(targets, t => t != TenantStatus.Installing)
            : Array.Empty<TenantStatus>();
}
