using Nexora.Domain.Common;

namespace Nexora.Domain.Provisioning;

/// <summary>
/// Modelo de negócio persistido (US-142) — pizzaria, hamburgueria, restaurante, lanchonete.
/// Substitui o catálogo estático de <see cref="ProvisioningTemplates"/> (que só sabia "PIZZERIA"
/// via <c>switch</c> em código) por dado editável pela Replay, sem deploy: exatamente a aplicação
/// direta de ADR-013 ("regra específica é configuração, nunca código"). <see cref="ConfigJson"/> e
/// <see cref="SeedsJson"/> guardam a mesma forma de dados que
/// <see cref="ProvisioningTemplate"/>/<see cref="ProvisioningConfigTemplate"/> descrevem, só que
/// serializada (Domain não referencia serializador — Application converte).
/// </summary>
public sealed class BusinessTemplate
{
    private BusinessTemplate() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Version { get; private set; } = 1;
    public string ConfigJson { get; private set; } = "{}";
    public string SeedsJson { get; private set; } = "{}";
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static BusinessTemplate Create(string code, string name, string configJson, string seedsJson)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("O código do modelo de negócio é obrigatório.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do modelo de negócio é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new BusinessTemplate
        {
            Id = IdGenerator.NewId(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name,
            Version = 1,
            ConfigJson = configJson,
            SeedsJson = seedsJson,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Atualiza o modelo pela Replay (US-142 §4 "Atualização de modelo") — incrementa a versão;
    /// tenants já provisionados guardam a versão aplicada em <c>tenant_config.template_version</c>
    /// e não são alterados retroativamente (cenário "tenants existentes não devem ser alterados").
    /// </summary>
    public void Update(string name, string configJson, string seedsJson)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do modelo de negócio é obrigatório.");

        Name = name;
        ConfigJson = configJson;
        SeedsJson = seedsJson;
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
