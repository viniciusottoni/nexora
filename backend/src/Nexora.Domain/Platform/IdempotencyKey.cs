using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Registro de idempotência de escrita (ADR-020). A chave é gerada pelo cliente quando a
/// intenção nasce, não a cada tentativa — por isso <see cref="Key"/> é string, não UUIDv7
/// da origem. <see cref="Key"/> continua sendo a PK isolada (não composta com tenant) porque a
/// chave já é globalmente única por construção (UUID aleatório gerado no cliente) — ver
/// <c>IdempotencyMiddleware</c> em Api.Edge/Api.Cloud para a justificativa completa.
/// </summary>
/// <remarks>
/// <b>Desvio deliberado do SQL literal do ADR-020</b> (que declara <c>tenant_id UUID NOT NULL</c>):
/// esta tabela também precisa suportar escritas de rotas sem tenant resolvido no momento da
/// requisição — pareamento de dispositivo (<c>POST /v1/devices/pair</c>, anônimo por design,
/// ADR-004), provisionamento de tenant (<c>POST /v1/platform/tenants</c>, ação de plataforma que
/// opera ANTES de o tenant existir) e registro/consumo de instalação (a instalação ainda não tem
/// credencial). Nenhuma dessas é "tenant divergente" — é "tenant ainda não existe" — por isso
/// <see cref="TenantId"/> é <c>Guid?</c> aqui, e a migration <c>AdjustIdempotencyKeyTenantScope</c>
/// (i) remove o <c>NOT NULL</c> da coluna e (ii) desliga RLS nesta tabela especificamente: com
/// <c>tenant_id</c> nulo, a política <c>tenant_id = current_tenant_id()</c> herdada de
/// <c>EnableRowLevelSecurity</c> negaria a escrita mesmo com <c>WITH CHECK</c> satisfeito por um
/// tenant real, porque <c>NULL = current_tenant_id()</c> nunca é verdadeiro (ADR-004, falha
/// fechada) — exatamente o comportamento certo para dado de negócio, mas errado para esta tabela
/// de plumbing cross-tenant. <c>idempotency_key</c> passa a ser tratada como raiz global (mesma
/// categoria de <c>tenant</c>/<c>unit_of_measure</c>), não como dado de negócio por tenant.
/// </remarks>
public sealed class IdempotencyKey
{
    private IdempotencyKey() { }

    public string Key { get; private set; } = string.Empty;

    /// <summary>Nulo quando a requisição não tinha tenant resolvido (ver <see cref="IdempotencyKey"/>).</summary>
    public Guid? TenantId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public string Status { get; private set; } = "IN_PROGRESS";
    public int? ResponseStatus { get; private set; }

    // TODO: value object tipado quando o formato de responseBody for definido — hoje é JSONB livre
    public string? ResponseBody { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public static IdempotencyKey Create(string key, Guid? tenantId, string endpoint, string requestHash, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("A chave de idempotência é obrigatória.");

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new DomainException("O endpoint da requisição idempotente é obrigatório.");

        return new IdempotencyKey
        {
            Key = key,
            TenantId = tenantId,
            Endpoint = endpoint,
            RequestHash = requestHash,
            Status = "IN_PROGRESS",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public void Complete(int responseStatus, string? responseBody)
    {
        Status = "COMPLETED";
        ResponseStatus = responseStatus;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Marca a reserva como abandonada (requisição original falhou com 5xx ou lançou uma exceção
    /// não tratada — ADR-020: "não armazena, permite nova tentativa real"). Não é
    /// <c>DELETE</c> físico de propósito: o papel de runtime do banco (<c>app_user_role</c>) nunca
    /// recebe privilégio de <c>DELETE</c> (Docs/Domain/10 §4, "soft delete é a única forma de
    /// remoção") — <c>FAILED</c> é o estado terminal que <c>IdempotencyStore.BeginAsync</c> (o
    /// <c>INSERT ... ON CONFLICT DO UPDATE</c>) reconhece como livre para reclamar com a mesma
    /// chave, exatamente como uma chave expirada.
    /// </summary>
    public void Abandon()
    {
        Status = "FAILED";
        ResponseStatus = null;
        ResponseBody = null;
    }
}
