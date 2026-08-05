using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Platform.SupportAccessTokens;

/// <summary>
/// US-145, cenário Gherkin "Nenhum acesso sem registro" — valida um token de suporte bruto
/// (recebido, por exemplo, num header <c>X-Support-Token</c>) contra a concessão gravada em
/// <c>support_access</c> para <paramref name="tenantId"/>. O chamador informa o tenant-alvo porque
/// a rota que consumiria este validador já o conhece (ex.: <c>/v1/platform/tenants/{id}/...</c>,
/// mesmo padrão de <c>RecordSupportAccessCommand</c>) — <c>support_access</c> tem RLS com
/// <c>USING</c> (não só <c>WITH CHECK</c>), então uma checagem "token pertence a QUALQUER tenant"
/// exigiria bypass de RLS (papel <c>platform_admin</c>, hoje sem nenhuma conexão da aplicação
/// usando-o) ou iteração por todos os tenants — nenhum dos dois necessário aqui: por construção,
/// perguntar "este token dá acesso ao tenant B" com o token de A nunca encontra linha nenhuma
/// (RLS filtra a linha de A antes mesmo da comparação de hash), o que É a garantia de isolamento
/// que o cenário "Isolamento" desta US pede — não uma coincidência de implementação.
/// </summary>
public interface ISupportAccessTokenValidator
{
    Task<Result<SupportAccessTokenValidationResult>> ValidateAsync(
        Guid tenantId, string rawToken, DateTimeOffset now, CancellationToken cancellationToken);
}

/// <summary>Concessão resolvida por um token válido — <see cref="SupportAccessId"/> serve de correlação para auditoria do uso.</summary>
public sealed record SupportAccessTokenValidationResult(Guid SupportAccessId, Guid TenantId, Guid? GrantedTo);
