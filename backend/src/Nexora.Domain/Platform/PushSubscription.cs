using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Assinatura de push de navegador (Web Push/VAPID, RFC 8291/8292) de um usuário num dispositivo —
/// US-081 §7 <c>POST /v1/notifications/subscribe</c>. Enviada e persistida SEMPRE na nuvem (US-081
/// §2: "o push é enviado pela nuvem, não pelo edge"), mesmo quando o navegador que se inscreveu está
/// operando contra o edge — o endpoint do provedor de push (FCM/Mozilla/etc.) só é alcançável com
/// internet, então não há razão para o edge guardar sua própria cópia.
/// </summary>
public sealed class PushSubscription
{
    private PushSubscription() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string P256dhKey { get; private set; } = string.Empty;
    public string AuthKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static PushSubscription Create(Guid tenantId, Guid userId, string endpoint, string p256dhKey, string authKey)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new DomainException("O endpoint da assinatura de push é obrigatório.");

        if (string.IsNullOrWhiteSpace(p256dhKey) || string.IsNullOrWhiteSpace(authKey))
            throw new DomainException("As chaves p256dh/auth da assinatura de push são obrigatórias.");

        var now = DateTimeOffset.UtcNow;

        return new PushSubscription
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            UserId = userId,
            Endpoint = endpoint,
            P256dhKey = p256dhKey,
            AuthKey = authKey,
            CreatedAt = now,
            LastSeenAt = now
        };
    }

    public void Touch()
    {
        LastSeenAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
    }
}
