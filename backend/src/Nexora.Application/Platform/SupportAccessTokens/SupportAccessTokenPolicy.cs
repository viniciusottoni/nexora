using Nexora.Domain.Platform;
using Nexora.Shared.Errors;

namespace Nexora.Application.Platform.SupportAccessTokens;

/// <summary>
/// US-145, cenário Gherkin "Nenhum acesso sem registro" — decisão PURA (sem banco) de se um token
/// de suporte pode ser usado agora, separada de <see cref="ISupportAccessTokenValidator"/> (que
/// resolve o hash e toca o banco) para ficar testável em <c>Nexora.UnitTests</c> sem mock de
/// infraestrutura, mesma exigência de cobertura de <c>Nexora.Domain</c> aplicada aqui por extensão
/// (ADR "Domain ≥ 90% sem mock de infraestrutura" do CLAUDE.md).
/// </summary>
public enum SupportAccessTokenStatus
{
    Valid,
    NotFound,
    Revoked,
    Expired,
}

public static class SupportAccessTokenPolicy
{
    /// <summary><paramref name="access"/> nulo representa hash desconhecido (token inválido/inexistente).</summary>
    public static SupportAccessTokenStatus Evaluate(SupportAccess? access, DateTimeOffset now)
    {
        if (access is null)
        {
            return SupportAccessTokenStatus.NotFound;
        }

        if (access.IsRevoked)
        {
            return SupportAccessTokenStatus.Revoked;
        }

        return access.IsExpired(now) ? SupportAccessTokenStatus.Expired : SupportAccessTokenStatus.Valid;
    }

    /// <summary>Mensagem/código (ADR-021) para todo status diferente de <see cref="SupportAccessTokenStatus.Valid"/>.</summary>
    public static (string Message, string Code) FailureFor(SupportAccessTokenStatus status) => status switch
    {
        SupportAccessTokenStatus.NotFound => ("Token de suporte inválido.", ApiErrorCodes.SupportAccessTokenNotFound),
        SupportAccessTokenStatus.Revoked => ("Token de suporte revogado.", ApiErrorCodes.SupportAccessTokenRevoked),
        SupportAccessTokenStatus.Expired => ("Token de suporte expirado.", ApiErrorCodes.SupportAccessTokenExpired),
        _ => throw new InvalidOperationException("Token válido não tem falha associada."),
    };
}
