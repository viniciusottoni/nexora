using Nexora.Domain.Common;

namespace Nexora.Domain.Finance;

/// <summary>
/// Funcionário — pode ou não estar vinculado a um usuário do sistema (app_user) — base do
/// custo de folha (RF-FIN).
/// </summary>
public sealed class Employee
{
    private Employee() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? StoreId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? RoleTitle { get; private set; }
    public string? Employment { get; private set; }
    public decimal Salary { get; private set; }
    public DateOnly? HiredAt { get; private set; }
    public DateOnly? TerminatedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static Employee Create(Guid tenantId, string name, Guid? storeId = null, Guid? userId = null, decimal salary = 0m)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do funcionário é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Employee
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            UserId = userId,
            Name = name,
            Salary = salary,
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
