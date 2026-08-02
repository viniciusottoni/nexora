using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Papel de autorização do tenant (ADR-023), com permissões em JSONB.</summary>
public sealed class Role
{
    private readonly List<UserRole> _userRoles = new();

    private Role() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    // TODO: value object tipado quando o formato de permissions for definido — hoje é JSONB livre
    public string Permissions { get; private set; } = "[]";

    public bool IsSystem { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static Role Create(Guid tenantId, string code, string name, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("O código do papel é obrigatório.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do papel é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Role
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            IsSystem = isSystem,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdatePermissions(string permissionsJson)
    {
        Permissions = permissionsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do papel é obrigatório.");

        Name = name;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        if (IsSystem)
            throw new DomainException("Papel de sistema não pode ser excluído.");

        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
