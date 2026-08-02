namespace Awaken.Contracts.Admin.MvpHealth;

/// <summary>
/// US-216: resposta consolidada do endpoint de saúde do MVP.
/// Agrega sinais de todos os domínios operacionais em uma visão única.
/// RN-003: OverallStatus nunca é "healthy" quando qualquer domínio está sem dados.
/// </summary>
public record MvpHealthStatusResponse(
    string OverallStatus,
    IReadOnlyList<DomainCardResponse> Domains,
    IReadOnlyList<string> P0Blockers,
    DateTime GeneratedAtUtc);
