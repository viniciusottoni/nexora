namespace Awaken.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; protected set; }
    public DateTime? DeletedAtUtc { get; protected set; }
    public Guid? CreatedByUserId { get; protected set; }
    public Guid? UpdatedByUserId { get; protected set; }
    public bool IsDeleted { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public void SoftDelete(Guid deletedByUserId, DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        UpdatedByUserId = deletedByUserId;
    }
}
