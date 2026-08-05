namespace Nexora.Infrastructure.Platform;

/// <summary>
/// Sufixo do domínio padrão da plataforma (ex.: <c>"plataforma.com.br"</c>, formando
/// <c>"{slug}.plataforma.com.br"</c>) — usado só por <see cref="TenantDomainRedirectResolver"/>
/// para decidir quando redirecionar (US-143 §3.1, "Redirecionamento do domínio padrão para o
/// próprio"). Sem valor configurado (nenhum <c>"Platform:DefaultDomainSuffix"</c> em
/// appsettings/env — o caso hoje em dev/CI e também em produção, este repositório não tem essa
/// convenção de DNS provisionada ainda; ver a nota do gap de <c>infra/cloud</c> no relatório desta
/// tarefa), o resolver nunca redireciona: mesmo idioma condicional de
/// <c>EmailOutboxOptions</c>/<c>WebPushOptions</c> (infraestrutura real plugável por configuração,
/// comportamento seguro por padrão sem ela).
/// </summary>
public sealed class PlatformDomainOptions
{
    public const string SectionName = "Platform";

    public string? DefaultDomainSuffix { get; set; }
}
