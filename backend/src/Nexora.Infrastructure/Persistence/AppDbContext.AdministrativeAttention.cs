using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

/// <summary>
/// Implementação de US-157 (Central operacional, auditoria e atalhos de suporte) de
/// <see cref="Application.Abstractions.Persistence.IApplicationDbContext"/> — ver docstring de
/// <c>IApplicationDbContext.AdministrativeAttention.cs</c>.
/// </summary>
public partial class AppDbContext
{
    public DbSet<Domain.Platform.AdministrativeAttentionAcknowledgement> AdministrativeAttentionAcknowledgements =>
        Set<Domain.Platform.AdministrativeAttentionAcknowledgement>();
}
