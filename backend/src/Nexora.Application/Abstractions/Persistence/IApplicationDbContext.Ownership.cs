using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Abstractions.Persistence;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — extensão de <see cref="IApplicationDbContext"/>
/// só com o que essa história precisa além do que já existia (DbSet de <c>OwnerInvites</c>/<c>Users</c>/
/// <c>UserRoles</c>/<c>Roles</c>/<c>EmailOutboxes</c>/<c>AuditLogs</c>/<c>DomainEvents</c> já estavam
/// declarados em <c>IApplicationDbContext.cs</c>). Arquivo próprio (não editar o principal — ver
/// docstring dele e a nota de coordenação entre agentes desta tarefa).
/// </summary>
public partial interface IApplicationDbContext
{
    /// <summary>US-155 — histórico imutável de transferência de titularidade (papel OWNER).</summary>
    DbSet<Domain.Platform.OwnershipTransfer> OwnershipTransfers { get; }

    /// <summary>
    /// Trava (<c>SELECT ... FOR UPDATE</c>) a linha do papel <c>OWNER</c> do tenant pelo restante da
    /// transação corrente — mecanismo de concorrência escolhido para
    /// <c>TransferTenantOwnershipCommandHandler</c> garantir "exatamente um proprietário principal"
    /// sem exigir uma migração de dado em <c>ProvisionTenantCommandHandler</c> (fora do escopo desta
    /// tarefa — ver relatório final). Duas transferências concorrentes para o MESMO tenant serializam
    /// nesta trava: a segunda só prossegue depois da primeira commitar/reverter, e nesse ponto lê o
    /// estado JÁ atualizado. Transferências de tenants DIFERENTES nunca se bloqueiam entre si (a
    /// trava é por linha, não por tabela). <c>FOR UPDATE</c> é sintaxe padrão SQL (não específica de
    /// provider), mas <c>Database.SqlQueryRaw</c> só está disponível em <c>Microsoft.EntityFrameworkCore.Relational</c>
    /// (proibido em <c>Application</c>, ADR-039) — por isso esta porta estreita, implementada em
    /// <c>AppDbContext</c> (Infrastructure), mesmo padrão de <see cref="IApplicationDbContext.TenantsMatchingSearchTerm"/>.
    /// Retorna <c>null</c> quando o tenant não tem papel OWNER configurado (tenant provisionado antes
    /// do template exigir OWNER, ou dado corrompido) — o handler trata isso como falha de negócio,
    /// nunca como exceção.
    /// </summary>
    Task<Guid?> LockOwnerRoleIdAsync(Guid tenantId, CancellationToken cancellationToken);
}
