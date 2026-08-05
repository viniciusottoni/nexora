namespace Nexora.Application.Abstractions.Platform;

/// <summary>
/// Porta usada por <c>Nexora.Api.Cloud.Infrastructure.Platform.TenantDomainRedirectMiddleware</c>
/// (US-143 §3.1, "Redirecionamento do domínio padrão para o próprio"). Separada da implementação
/// (que mora em <c>Nexora.Application.TenantDomains.TenantDomainRedirectResolver</c>, não em
/// Infrastructure — só le de <see cref="Nexora.Application.Abstractions.Persistence.IApplicationDbContext"/>,
/// sem I/O externo que justifique um adapter) para o middleware poder ser testado com um duplo em
/// memória, no mesmo espírito de <c>FakeIdempotencyStore</c> em
/// <c>Nexora.ApiTests.Idempotency.IdempotencyMiddlewareTests</c>.
/// </summary>
public interface ITenantDomainRedirectResolver
{
    /// <summary>
    /// Domínio próprio (ativo, <c>IsPrimary</c>) do tenant a que <paramref name="normalizedHost"/>
    /// resolve, quando diferente do próprio <paramref name="normalizedHost"/> — ou <c>null</c>
    /// quando o host não resolve a nenhum tenant, ou já é o domínio primário do tenant (nada para
    /// redirecionar).
    /// </summary>
    Task<string?> ResolvePrimaryDomainAsync(string normalizedHost, CancellationToken cancellationToken);
}
