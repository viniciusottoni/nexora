using Nexora.Domain.Common;

namespace Nexora.Domain.Catalog;

/// <summary>
/// Preço vigente de uma variante em um canal — nunca é editado após criado; uma mudança de
/// preço cria uma nova linha com <see cref="ValidFrom"/> e fecha a anterior (<see cref="Close"/>).
/// </summary>
public sealed class Price
{
    private Price() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid VariantId { get; private set; }
    public Channel Channel { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }

    public ProductVariant Variant { get; private set; } = null!;

    public static Price Create(Guid tenantId, Guid variantId, Channel channel, decimal amount, Guid? createdBy = null, DateTimeOffset? validFrom = null)
    {
        if (amount < 0)
            throw new DomainException("O preço não pode ser negativo.");

        var now = DateTimeOffset.UtcNow;

        return new Price
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            VariantId = variantId,
            Channel = channel,
            Amount = amount,
            ValidFrom = validFrom ?? now,
            CreatedBy = createdBy,
            CreatedAt = now
        };
    }

    public void Close(DateTimeOffset validTo)
    {
        if (ValidTo is not null)
            throw new DomainException("Este preço já está encerrado.");

        if (validTo <= ValidFrom)
            throw new DomainException("O fim da vigência deve ser posterior ao início.");

        ValidTo = validTo;
    }
}
