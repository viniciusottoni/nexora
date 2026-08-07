using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — histórico IMUTÁVEL de transferência de
/// titularidade (papel OWNER) entre usuários de um tenant. Registro de fato, sem regra de negócio
/// própria além de existir (mesma natureza de <see cref="AuditLog"/>/<see cref="DomainEvent"/>) —
/// criado uma vez por <c>TransferTenantOwnershipCommandHandler</c>, nunca mutado depois.
/// </summary>
/// <remarks>
/// Decisão de modelagem (documentada no relatório da tarefa): o histórico específico de
/// transferência de titularidade merece uma entidade própria, em vez de só <see cref="AuditLog"/>/
/// <see cref="DomainEvent"/> genéricos, porque a tela administrativa (<c>tenant-ownership-section.tsx</c>)
/// precisa listar "quem foi dono de quando até quando" com uma projeção estável e tipada
/// (<see cref="PreviousOwnerUserId"/>/<see cref="NewOwnerUserId"/>/<see cref="PreviousKeptAsAdmin"/>)
/// sem depender do formato JSONB livre de <see cref="AuditLog.Before"/>/<see cref="AuditLog.After"/>.
/// </remarks>
public sealed class OwnershipTransfer
{
    private OwnershipTransfer() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PreviousOwnerUserId { get; private set; }
    public Guid NewOwnerUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Se o proprietário anterior manteve algum papel administrativo (nunca o OWNER — esse é sempre removido).</summary>
    public bool PreviousKeptAsAdmin { get; private set; }

    public Guid? ActorId { get; private set; }
    public DateTimeOffset TransferredAt { get; private set; }

    public static OwnershipTransfer Create(
        Guid tenantId,
        Guid previousOwnerUserId,
        Guid newOwnerUserId,
        string reason,
        bool previousKeptAsAdmin,
        Guid? actorId,
        DateTimeOffset transferredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo da transferência de titularidade é obrigatório.");

        if (previousOwnerUserId == newOwnerUserId)
            throw new DomainException("O novo proprietário precisa ser diferente do atual.");

        return new OwnershipTransfer
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            PreviousOwnerUserId = previousOwnerUserId,
            NewOwnerUserId = newOwnerUserId,
            Reason = reason.Trim(),
            PreviousKeptAsAdmin = previousKeptAsAdmin,
            ActorId = actorId,
            TransferredAt = transferredAt
        };
    }
}
