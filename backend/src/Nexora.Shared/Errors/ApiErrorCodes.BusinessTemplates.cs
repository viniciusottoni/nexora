namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do catálogo de modelos de negócio (US-142, ADR-021).
/// </summary>
/// <remarks>
/// Por instrução explícita desta tarefa, <c>ResultExtensions.MapErrorCode</c> (Api.Edge e
/// Api.Cloud) NÃO foi editado aqui — o código abaixo cai hoje no <c>default</c> daquele switch
/// (500 Internal Server Error) até alguém adicionar a entrada. Mapeamento HTTP pretendido, para
/// quem for adicionar:
/// <list type="table">
/// <item><term><see cref="BusinessTemplateNotFound"/></term><description>404 Not Found, recoverable=false, requiresAuthorization=false — mesma convenção de <c>TenantNotFound</c>/<c>TenantDomainNotFound</c>. Usado tanto por <c>ProvisionTenantCommandHandler</c> (código inexistente/inativo) quanto por <c>GetBusinessTemplateQueryHandler</c>/<c>UpdateBusinessTemplateCommandHandler</c>.</description></item>
/// </list>
/// </remarks>
public static partial class ApiErrorCodes
{
    /// <summary>Nenhum <c>business_template</c> ativo com o código informado — provisionamento com modelo desconhecido (US-142 §4, cenário implícito de robustez) ou manutenção do catálogo apontando para um código que não existe.</summary>
    public const string BusinessTemplateNotFound = "BUSINESS_TEMPLATE_NOT_FOUND";
}
