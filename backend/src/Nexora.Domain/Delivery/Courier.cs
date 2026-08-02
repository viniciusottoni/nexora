using Nexora.Domain.Common;

namespace Nexora.Domain.Delivery;

/// <summary>
/// Entregador — próprio da loja ou terceirizado — pode ou não estar vinculado a um usuário
/// do sistema.
/// </summary>
public sealed class Courier
{
    private Courier() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Vehicle { get; private set; }
    public string? Plate { get; private set; }
    public bool IsOwn { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static Courier Create(Guid tenantId, Guid storeId, string name, Guid? userId = null, bool isOwn = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do entregador é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Courier
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            UserId = userId,
            Name = name,
            IsOwn = isOwn,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
