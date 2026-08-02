using Nexora.Domain.Common;

namespace Nexora.Domain.Delivery;

/// <summary>
/// Endereço de entrega vinculado a um cliente — pode pertencer a uma área de entrega (RF-DLV).
/// </summary>
public sealed class CustomerAddress
{
    private CustomerAddress() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? ZoneId { get; private set; }
    public string? Label { get; private set; }
    public string Street { get; private set; } = string.Empty;
    public string? Number { get; private set; }
    public string? Complement { get; private set; }
    public string? District { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? State { get; private set; }
    public string? Zip { get; private set; }
    public string? Reference { get; private set; }
    public decimal? Lat { get; private set; }
    public decimal? Lng { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static CustomerAddress Create(Guid tenantId, Guid customerId, string street, string city, Guid? zoneId = null)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("A rua do endereço é obrigatória.");

        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("A cidade do endereço é obrigatória.");

        var now = DateTimeOffset.UtcNow;

        return new CustomerAddress
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            CustomerId = customerId,
            ZoneId = zoneId,
            Street = street,
            City = city,
            IsDefault = false,
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
