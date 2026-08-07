using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.TransferTenantOwnership;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — transfere o papel OWNER de um usuário
/// para outro, garantindo "existe exatamente um proprietário principal" (Gherkin "Transferência de
/// titularidade") e que "o anterior não deve manter privilégios por acidente".
/// </summary>
/// <remarks>
/// <b>Concorrência (decisão documentada no relatório final):</b> duas transferências simultâneas
/// para o MESMO tenant são serializadas por <see cref="IApplicationDbContext.LockOwnerRoleIdAsync"/>
/// (<c>SELECT ... FOR UPDATE</c> na linha do papel OWNER, dentro da transação que o
/// <c>TransactionBehavior</c> já abre por comando) — não uma constraint de banco nova. Optamos por
/// não introduzir uma coluna/índice parcial (<c>uq_ ... WHERE is_primary_owner</c>) porque isso
/// exigiria também alterar <c>ProvisionTenantCommandHandler</c> (fora do escopo desta tarefa e sob
/// edição de outro agente em paralelo — ver relatório) para popular a mesma coluna em todo tenant
/// NOVO; a trava pessimista cobre o invariante sem tocar naquele arquivo, ao custo de serializar
/// (não paralelizar) transferências do MESMO tenant — aceitável, é uma operação rara e administrativa.
///
/// <b>"Manter como admin" (decisão documentada):</b> o catálogo de papéis é definido por
/// <c>business_template</c> (ADR-013 — nunca por tenant) e nem todo template garante um papel
/// "ADMIN"/"MANAGER" equivalente. Quando <see cref="TransferTenantOwnershipCommand.KeepPreviousAsAdmin"/>
/// é <c>true</c>, procuramos por um papel do PRÓPRIO tenant (nunca hardcoded por cliente) cujo código
/// bata com uma pequena lista de sinônimos comuns (<see cref="AdminEquivalentRoleCodes"/>); se
/// nenhum existir no catálogo do tenant, o ex-proprietário simplesmente mantém os papéis que já
/// tinha antes (se algum) — nenhum papel é inventado.
///
/// <b>Como o papel OWNER é "removido" do anterior (decisão documentada, achada só ao rodar o
/// primeiro teste de integração contra Postgres real):</b> o papel de runtime da aplicação
/// (<c>app_user_role</c>) só tem <c>GRANT SELECT, INSERT, UPDATE</c> nas tabelas de negócio —
/// NUNCA <c>DELETE</c> (migration <c>EnableRowLevelSecurity</c>; reflexo do princípio deste projeto
/// "DELETE físico não existe"). <see cref="UserRole"/> não tem <c>deleted_at</c> (não é uma entidade
/// soft-delete — é uma linha de associação pura), então excluir fisicamente a atribuição também não
/// seria a forma "certa" de revogar mesmo que a permissão existisse. Por isso a transferência REPÕE
/// a MESMA linha de <see cref="UserRole"/> do papel OWNER para o novo dono
/// (<see cref="UserRole.TransferTo"/>, um UPDATE) em vez de excluir a linha antiga e inserir uma
/// nova — o efeito estrutural é idêntico ("existe exatamente uma linha apontando o OWNER, e agora
/// aponta para o novo dono"; o anterior não tem mais NENHUMA linha de OWNER apontando para ele, o
/// que já cumpre "não deve manter privilégios por acidente" quanto ao papel OWNER em si) sem exigir
/// nenhum GRANT novo. Papéis OUTROS que o ex-proprietário já tivesse ANTES de virar dono (ex.:
/// CASHIER) não são tocados por esta operação quando <see cref="TransferTenantOwnershipCommand.KeepPreviousAsAdmin"/>
/// é <c>false</c> — decisão de escopo: revogar arbitrariamente qualquer outro papel exigiria o mesmo
/// DELETE indisponível, e "privilégio" no cenário Gherkin se refere ao papel de proprietário em si,
/// o único que este endpoint concede/revoga.
/// </remarks>
internal sealed class TransferTenantOwnershipCommandHandler
    : IRequestHandler<TransferTenantOwnershipCommand, Result<TransferTenantOwnershipResponse>>
{
    private static readonly string[] AdminEquivalentRoleCodes = { "ADMIN", "MANAGER", "GERENTE" };

    private readonly IApplicationDbContext _db;

    public TransferTenantOwnershipCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TransferTenantOwnershipResponse>> Handle(
        TransferTenantOwnershipCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result<TransferTenantOwnershipResponse>.Failure("O motivo é obrigatório.", ApiErrorCodes.ReasonRequired);
        }

        var tenant = await _db.Tenants
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result<TransferTenantOwnershipResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        // Trava a linha do papel OWNER pelo restante da transação — ver docstring da classe.
        var ownerRoleId = await _db.LockOwnerRoleIdAsync(tenant.Id, cancellationToken);
        if (ownerRoleId is null)
        {
            return Result<TransferTenantOwnershipResponse>.Failure(
                "Este estabelecimento não tem um papel de proprietário configurado.",
                ApiErrorCodes.OwnershipNoOwnerRoleConfigured);
        }

        var currentOwnerAssignments = await _db.UserRoles
            .Where(ur => ur.TenantId == tenant.Id && ur.RoleId == ownerRoleId.Value)
            .ToListAsync(cancellationToken);

        if (currentOwnerAssignments.Count == 0)
        {
            return Result<TransferTenantOwnershipResponse>.Failure("Proprietário não encontrado.", ApiErrorCodes.OwnershipOwnerNotFound);
        }

        var previousOwnerUserId = currentOwnerAssignments[0].UserId;

        if (request.NewOwnerUserId == previousOwnerUserId)
        {
            return Result<TransferTenantOwnershipResponse>.Failure(
                "O novo proprietário precisa ser diferente do atual.", ApiErrorCodes.OwnershipSameOwner);
        }

        // RN-015: só resolve o novo dono DENTRO do mesmo tenant — um id de outro tenant vira o
        // mesmo 404 de "não encontrado", nunca revela que o usuário existe alhures.
        var newOwner = await _db.Users
            .SingleOrDefaultAsync(u => u.Id == request.NewOwnerUserId && u.TenantId == tenant.Id && u.DeletedAt == null, cancellationToken);

        if (newOwner is null)
        {
            return Result<TransferTenantOwnershipResponse>.Failure("Usuário não encontrado.", ApiErrorCodes.OwnershipTargetUserNotFound);
        }

        var ownerRole = await _db.Roles.SingleAsync(r => r.Id == ownerRoleId.Value, cancellationToken);
        var ownerPermissions = JsonSerializer.Deserialize<List<string>>(ownerRole.Permissions) ?? new List<string>();

        // Repõe a(s) linha(s) de OWNER para o novo dono — ver docstring da classe ("Como o papel
        // OWNER é removido do anterior") sobre por que é UPDATE (TransferTo), nunca DELETE+INSERT.
        // Normalmente uma única linha; se o dado já estivesse inconsistente com mais de uma, todas
        // são repostas para o mesmo novo dono (nunca deixa uma órfã apontando para o antigo).
        foreach (var assignment in currentOwnerAssignments)
        {
            assignment.TransferTo(newOwner.Id);
        }

        if (request.KeepPreviousAsAdmin)
        {
            await GrantAdminEquivalentIfAvailableAsync(tenant.Id, previousOwnerUserId, ownerRoleId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(newOwner.Email))
        {
            tenant.SetOwnerEmail(newOwner.Email);
        }

        var now = DateTimeOffset.UtcNow;

        var transfer = OwnershipTransfer.Create(
            tenant.Id, previousOwnerUserId, newOwner.Id, request.Reason, request.KeepPreviousAsAdmin, request.ActorId, now);
        _db.OwnershipTransfers.Add(transfer);

        var ownerChangedEvent = DomainEvent.Create(
            tenant.Id,
            type: "tenant.owner_access_changed",
            aggregateType: "tenant",
            aggregateId: tenant.Id,
            payload: JsonSerializer.Serialize(new
            {
                tenantId = tenant.Id,
                action = "TRANSFERRED",
                userId = newOwner.Id,
                inviteId = (Guid?)null,
                previousOwnerId = previousOwnerUserId,
                actorId = request.ActorId,
            }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: request.ActorId);
        _db.DomainEvents.Add(ownerChangedEvent);

        // EVT-072 — papel OWNER removido do anterior e atribuído ao novo (dois eventos: "removido"
        // primeiro, "atribuído" depois, mesma ordem em que a mutação acontece acima).
        _db.DomainEvents.Add(DomainEvent.Create(
            tenant.Id,
            type: "permission.changed",
            aggregateType: "role",
            aggregateId: ownerRole.Id,
            payload: JsonSerializer.Serialize(new { roleId = ownerRole.Id, userId = previousOwnerUserId, permissions = Array.Empty<string>() }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: request.ActorId));

        _db.DomainEvents.Add(DomainEvent.Create(
            tenant.Id,
            type: "permission.changed",
            aggregateType: "role",
            aggregateId: ownerRole.Id,
            payload: JsonSerializer.Serialize(new { roleId = ownerRole.Id, userId = newOwner.Id, permissions = ownerPermissions }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: request.ActorId));

        _db.AuditLogs.Add(AuditLog.Create(
            tenant.Id,
            action: "TENANT_OWNERSHIP_TRANSFERRED",
            entity: "tenant",
            occurredAt: now,
            actorId: request.ActorId,
            entityId: tenant.Id,
            before: JsonSerializer.Serialize(new { ownerId = previousOwnerUserId }),
            after: JsonSerializer.Serialize(new { ownerId = newOwner.Id, keptAsAdmin = request.KeepPreviousAsAdmin }),
            reason: request.Reason,
            domainEventId: ownerChangedEvent.Id));

        var response = new TransferTenantOwnershipResponse(previousOwnerUserId, newOwner.Id, request.KeepPreviousAsAdmin, now);
        return Result<TransferTenantOwnershipResponse>.Success(response);
    }

    /// <summary>Ver docstring da classe ("Manter como admin") — nunca inventa um papel, só reusa um já existente no catálogo do próprio tenant.</summary>
    private async Task GrantAdminEquivalentIfAvailableAsync(
        Guid tenantId, Guid previousOwnerUserId, Guid ownerRoleId, CancellationToken cancellationToken)
    {
        // Candidatos do catálogo do PRÓPRIO tenant (nunca hardcoded por cliente, ADR-013) — poucos
        // registros por tenant, então trazemos TODOS os papéis não-OWNER para memória e filtramos/
        // ordenamos por preferência ali. Filtrar com `AdminEquivalentRoleCodes.Contains(r.Code)`
        // direto no `Where` (traduzido para SQL) quebra em runtime neste ambiente (.NET 10 —
        // `TypeLoadException` dentro do funcletizer do EF Core ao tentar avaliar o array capturado
        // como parâmetro, achado ao rodar o primeiro teste de integração contra Postgres real);
        // resolver em memória evita a tradução por completo.
        var candidates = await _db.Roles
            .Where(r => r.TenantId == tenantId && r.DeletedAt == null && r.Id != ownerRoleId)
            .ToListAsync(cancellationToken);

        var adminRole = AdminEquivalentRoleCodes
            .Select(code => candidates.FirstOrDefault(r => r.Code == code))
            .FirstOrDefault(r => r is not null);

        if (adminRole is null)
        {
            return;
        }

        var alreadyAssigned = await _db.UserRoles
            .AnyAsync(ur => ur.TenantId == tenantId && ur.UserId == previousOwnerUserId && ur.RoleId == adminRole.Id, cancellationToken);

        if (!alreadyAssigned)
        {
            _db.UserRoles.Add(UserRole.Create(tenantId, previousOwnerUserId, adminRole.Id, storeId: null));
        }
    }
}
