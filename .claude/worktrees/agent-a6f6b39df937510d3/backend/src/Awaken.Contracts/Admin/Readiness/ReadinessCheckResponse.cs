namespace Awaken.Contracts.Admin.Readiness;

/// <summary>
/// US-218: um único check de readiness (configuração, build ou CI).
/// RN-001: nunca carrega o valor real verificado — apenas nome, status e descrição segura.
/// </summary>
public record ReadinessCheckResponse(
    string Name,
    string Category,
    string Status,
    string Description,
    bool IsBlocker,
    DateTime CheckedAtUtc);
