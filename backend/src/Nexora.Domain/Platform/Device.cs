using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Dispositivo físico pareado a uma loja (POS, KDS, garçom, tablet, host de impressão).</summary>
public sealed class Device
{
    private readonly List<AuthSession> _sessions = new();

    private Device() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public DeviceType Type { get; private set; }
    public string Fingerprint { get; private set; } = string.Empty;
    public string? SecretHash { get; private set; }
    public Guid? StationId { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// US-042/US-045/US-047 (filtro de praça, som e modo pico do KDS, todos "por dispositivo") —
    /// JSONB livre, mesma convenção de <see cref="TenantConfig.Thresholds"/>: nenhum desses três
    /// formatos tem hoje uma tela própria de configuração, então um esquema tipado seria
    /// especulativo. Namespace por feature dentro do objeto (ex.: <c>{"kds":{"stationIds":[...],
    /// "sound":{...},"peakMode":{...}}}</c>) evita colisão entre features que gravam preferências
    /// do mesmo dispositivo em momentos diferentes.
    /// </summary>
    public string Preferences { get; private set; } = "{}";
    public DateTimeOffset? LastSeenAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Store Store { get; private set; } = null!;
    public IReadOnlyCollection<AuthSession> Sessions => _sessions.AsReadOnly();
    public AuthAttempt? AuthAttempt { get; private set; }

    public static Device Create(Guid tenantId, Guid storeId, string label, DeviceType type, string fingerprint, Guid? stationId = null)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("O rótulo do dispositivo é obrigatório.");

        if (string.IsNullOrWhiteSpace(fingerprint))
            throw new DomainException("A impressão digital (fingerprint) do dispositivo é obrigatória.");

        var now = DateTimeOffset.UtcNow;

        return new Device
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            Label = label,
            Type = type,
            Fingerprint = fingerprint,
            StationId = stationId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkSeen()
    {
        LastSeenAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Renomeia o rótulo de apresentação do dispositivo — porta de DeviceRegistry.rename (device-registry.ts).</summary>
    public void Rename(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("Informe um nome para o dispositivo.");

        var trimmed = label.Trim();
        if (trimmed.Length > 100)
            throw new DomainException("Nome do dispositivo deve ter até 100 caracteres.");

        Label = trimmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Define o hash do segredo do dispositivo no momento do pareamento (o segredo em claro é
    /// devolvido uma única vez ao chamador e nunca persistido) — porta do campo secretHash
    /// preenchido em DeviceRegistry.pair (device-registry.ts).
    /// </summary>
    public void SetSecret(string secretHash)
    {
        if (string.IsNullOrWhiteSpace(secretHash))
            throw new DomainException("O segredo do dispositivo é obrigatório.");

        SecretHash = secretHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Autoriza novamente uma instalação já conhecida pela mesma impressão digital. A posse de
    /// um código de pareamento válido permite rotacionar o segredo sem criar outro registro para
    /// o mesmo dispositivo.
    /// </summary>
    public void Reauthorize(Guid storeId, string label, DeviceType type, string secretHash)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("Informe um nome para o dispositivo.");

        var trimmedLabel = label.Trim();
        if (trimmedLabel.Length > 100)
            throw new DomainException("Nome do dispositivo deve ter até 100 caracteres.");

        if (string.IsNullOrWhiteSpace(secretHash))
            throw new DomainException("O segredo do dispositivo é obrigatório.");

        if (StoreId != storeId || Type != type)
            StationId = null;

        StoreId = storeId;
        Label = trimmedLabel;
        Type = type;
        SecretHash = secretHash;
        IsActive = true;
        // Um dispositivo excluído (US do gestor: "não quero mais ver na lista") continua com o
        // mesmo fingerprint reservado (uq_device_fingerprint) — posse de um código de pareamento
        // válido para essa instalação física deve reviver o mesmo registro, não ficar bloqueada
        // por ele estar soft-deleted.
        DeletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Substitui o JSON de preferências por completo — a MESCLA de campos (ex.: atualizar só
    /// <c>kds.sound</c> sem apagar <c>kds.stationIds</c> já gravado) é responsabilidade da
    /// Application (<c>UpdateDevicePreferencesCommandHandler</c>), porque só ela sabe interpretar o
    /// formato; o Domain só garante que o valor gravado é sempre um JSON válido não vazio.
    /// </summary>
    public void UpdatePreferences(string preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
            throw new DomainException("Preferências do dispositivo não podem ser vazias.");

        Preferences = preferencesJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AssignStation(Guid? stationId)
    {
        StationId = stationId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
