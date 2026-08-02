using Nexora.Domain.Common;

namespace Nexora.Domain.Delivery;

/// <summary>
/// Cliente identificado por telefone — atendido por qualquer canal (delivery, takeout, mesa).
/// Base de recorrência e histórico consolidado (RF-CRM).
/// </summary>
public sealed class Customer
{
    private Customer() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Document { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? AnonymizedAt { get; private set; }
    public DateTimeOffset? LastOrderAt { get; private set; }
    public int OrdersCount { get; private set; }
    public decimal TotalSpent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static Customer Create(Guid tenantId, string name, string phone, string? email = null, string? document = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do cliente é obrigatório.");

        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("O telefone do cliente é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Customer
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Name = name,
            Phone = phone,
            Email = email,
            Document = document,
            OrdersCount = 0,
            TotalSpent = 0m,
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
