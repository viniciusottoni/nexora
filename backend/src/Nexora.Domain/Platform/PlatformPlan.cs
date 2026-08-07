using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — catálogo versionado de planos comerciais
/// mantido pela Replay (<c>platform_plan</c>), sem <c>tenant_id</c>/RLS: mesma exceção legítima já
/// usada por <see cref="Nexora.Domain.Provisioning.BusinessTemplate"/> (ADR-013 — catálogo da
/// plataforma, não dado de um estabelecimento). Substitui o campo <see cref="Tenant.Plan"/> como
/// string livre por um código validado contra uma linha real e ativa deste catálogo — nenhuma
/// camada pode mais atribuir um plano que não exista ou que tenha sido desativado (RN-016).
/// </summary>
/// <remarks>
/// <see cref="CapabilitiesJson"/>/<see cref="LimitsJson"/> guardam JSON livre (mesmo padrão de
/// <see cref="TenantConfig"/>/<see cref="Nexora.Domain.Provisioning.BusinessTemplate"/> — Domain não
/// referencia serializador algum); a Application interpreta/deserializa. <see cref="Version"/>
/// incrementa a cada <see cref="Update"/> (mesma convenção de
/// <see cref="Nexora.Domain.Provisioning.BusinessTemplate.Update"/>) e é copiada para
/// <c>tenant_config</c> no momento da reconciliação, permitindo detectar divergência quando o
/// catálogo muda depois que um tenant já teve suas capacidades aplicadas.
/// </remarks>
public sealed class PlatformPlan
{
    private PlatformPlan() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Version { get; private set; } = 1;

    /// <summary>JSON de array de strings — capacidades habilitadas por este plano (ex.: <c>["multi_store","delivery"]</c>).</summary>
    public string CapabilitiesJson { get; private set; } = "[]";

    /// <summary>JSON de objeto — limites numéricos/textuais do plano (ex.: <c>{"maxStores":1}</c>).</summary>
    public string LimitsJson { get; private set; } = "{}";

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static PlatformPlan Create(string code, string name, string capabilitiesJson, string limitsJson)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("O código do plano é obrigatório.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do plano é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new PlatformPlan
        {
            Id = IdGenerator.NewId(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name,
            Version = 1,
            CapabilitiesJson = string.IsNullOrWhiteSpace(capabilitiesJson) ? "[]" : capabilitiesJson,
            LimitsJson = string.IsNullOrWhiteSpace(limitsJson) ? "{}" : limitsJson,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Atualiza capacidades/limites do plano — incrementa a versão (mesmo padrão de <see cref="Nexora.Domain.Provisioning.BusinessTemplate.Update"/>); tenants já reconciliados numa versão anterior passam a divergir até serem reconciliados de novo.</summary>
    public void Update(string name, string capabilitiesJson, string limitsJson)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do plano é obrigatório.");

        Name = name;
        CapabilitiesJson = string.IsNullOrWhiteSpace(capabilitiesJson) ? "[]" : capabilitiesJson;
        LimitsJson = string.IsNullOrWhiteSpace(limitsJson) ? "{}" : limitsJson;
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Desativa o plano — código desativado não pode mais ser atribuído a novo tenant nem em mudança de plano (US-154 §3.1); tenants que já o possuem não são afetados retroativamente.</summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
