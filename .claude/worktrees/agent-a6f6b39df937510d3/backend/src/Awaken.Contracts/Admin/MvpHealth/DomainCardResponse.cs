namespace Awaken.Contracts.Admin.MvpHealth;

/// <summary>
/// US-216: card de status de um domínio operacional no dashboard de saúde do MVP.
/// RN-003: status nunca é "healthy" quando não há dado real disponível — use "no_data".
/// </summary>
public record DomainCardResponse(
    string Key,
    string Label,
    string Status,
    string? Description,
    string? DetailUrl,
    DateTime? LastCheckedUtc);
