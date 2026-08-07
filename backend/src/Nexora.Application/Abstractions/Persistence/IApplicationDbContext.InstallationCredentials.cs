using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Abstractions.Persistence;

/// <summary>US-156 · Recuperação do provisionamento e token de instalação. Partial file NOVO (ver docstring de <see cref="IApplicationDbContext"/>) para não colidir com outras histórias da E-15 editando o arquivo principal em paralelo.</summary>
public partial interface IApplicationDbContext
{
    /// <summary>Histórico de credenciais de instalação rotacionadas (US-156) — nunca o segredo bruto, só <c>TokenHash</c>.</summary>
    DbSet<Domain.Platform.InstallationCredential> InstallationCredentials { get; }

    /// <summary>
    /// US-156, cenário de concorrência ("duas reemissões simultâneas da mesma instalação devem
    /// deixar só UMA credencial válida") — bloqueio pessimista (<c>SELECT ... FOR UPDATE</c>) da
    /// linha de <c>edge_installation</c> durante toda a janela de revogar-o-pendente-e-emitir-o-novo,
    /// para que duas reemissões concorrentes serializem: a segunda transação só enxerga o estado já
    /// rotacionado pela primeira depois que ela tiver COMMITADO (<c>TransactionBehavior</c> já
    /// envolve todo comando em uma transação real — ver docstring da classe), e portanto revoga a
    /// credencial que a primeira acabou de emitir antes de emitir a sua própria.
    /// <c>FOR UPDATE</c> é SQL padrão (não é sintaxe específica de provider), mas
    /// <c>FromSqlInterpolated</c> sobre um <c>DbSet&lt;T&gt;</c> mapeado só está disponível a partir
    /// de <c>Microsoft.EntityFrameworkCore.Relational</c> (pacote exclusivo de Infrastructure,
    /// ADR-039) — por isso esta porta estreita, implementada em <c>AppDbContext</c>, mesmo padrão de
    /// <see cref="AuditLogsWithMinAmount"/>/<see cref="TenantsMatchingSearchTerm"/>.
    /// </summary>
    Task<Domain.Platform.EdgeInstallation?> LockEdgeInstallationForUpdateAsync(
        Guid installationId, CancellationToken cancellationToken);
}
