using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>Fornecedor de insumos, referenciado por <see cref="Ingredient"/> e <see cref="Purchase"/>.</summary>
public sealed class Supplier
{
    private Supplier() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Document { get; private set; }
    // TODO: tipar quando o formato for definido
    public string? Contact { get; private set; }
    public int LeadTimeDays { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static Supplier Create(Guid tenantId, string name, string? document = null, string? contact = null, int leadTimeDays = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do fornecedor é obrigatório.");

        if (leadTimeDays < 0)
            throw new DomainException("O prazo de entrega do fornecedor não pode ser negativo.");

        var now = DateTimeOffset.UtcNow;

        return new Supplier
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Name = name,
            Document = document,
            Contact = contact,
            LeadTimeDays = leadTimeDays,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
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
