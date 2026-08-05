namespace Nexora.Contracts.Platform;

/// <summary>
/// Contratos de atualização controlada do parque (US-146 §7) — vivem em <c>Nexora.Contracts</c>
/// (ADR-039: só referencia <c>Nexora.Domain</c>).
/// </summary>
public sealed record PublishReleaseRequest(string Version, int RolloutPercent, string? Notes);

public sealed record ReleaseResponse(
    Guid Id,
    string Version,
    int RolloutPercent,
    string? Notes,
    DateTimeOffset PublishedAt,
    Guid? PublishedBy);

/// <summary>Corpo de <c>POST /v1/platform/releases</c> — espelha o exemplo do US-146 §7.</summary>
public sealed record PublishReleaseResponse(ReleaseResponse Release);

/// <summary>
/// <c>GET /v1/platform/releases/{version}/rollout</c> — US-146 §7: contagem do parque inteiro
/// (todos os tenants) para a versão publicada. <c>Updated</c> conta instalações já em
/// <c>Version == version</c>; <c>Failed</c> conta <c>LastUpdateStatus</c> em
/// <c>Failed</c>/<c>RolledBack</c> na última tentativa registrada PARA ESTA versão-alvo;
/// <c>Pending</c> é o restante (ainda não tentou, ou tentativa <c>Deferred</c>/<c>InProgress</c>).
/// </summary>
public sealed record ReleaseRolloutResponse(int Total, int Updated, int Failed, int Pending);
