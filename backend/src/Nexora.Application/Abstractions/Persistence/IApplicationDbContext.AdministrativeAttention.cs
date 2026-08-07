using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Abstractions.Persistence;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — porta do único <c>DbSet</c> novo
/// desta história. Partial file NOVO (mesma convenção de <c>IApplicationDbContext.Plans.cs</c>/
/// <c>IApplicationDbContext.Ownership.cs</c>) para não colidir com o corpo principal.
/// </summary>
public partial interface IApplicationDbContext
{
    DbSet<Domain.Platform.AdministrativeAttentionAcknowledgement> AdministrativeAttentionAcknowledgements { get; }
}
