namespace Nexora.Contracts.Auth;

/// <summary>Resumo do usuário autenticado devolvido nas respostas de login/refresh.</summary>
public sealed record AuthenticatedUserSummary(Guid Id, string Name);

/// <summary>Resumo do tenant devolvido nas respostas de login por senha/refresh (cloud).</summary>
public sealed record AuthenticatedTenantSummary(Guid Id, string Name);

/// <summary>Resumo de quem concedeu uma autorização pontual (ADR-023).</summary>
public sealed record AuthorizedBySummary(Guid Id, string Name);
