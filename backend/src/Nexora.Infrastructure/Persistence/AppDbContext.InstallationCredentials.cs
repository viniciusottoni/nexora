using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

/// <summary>US-156 · Recuperação do provisionamento e token de instalação. Partial file NOVO (ver docstring de <see cref="AppDbContext"/>/convenção de <c>AppDbContext.Platform.cs</c>) para não colidir com outras histórias da E-15 editando os arquivos principais em paralelo.</summary>
public partial class AppDbContext
{
    public DbSet<Domain.Platform.InstallationCredential> InstallationCredentials => Set<Domain.Platform.InstallationCredential>();

    /// <inheritdoc cref="Application.Abstractions.Persistence.IApplicationDbContext.LockEdgeInstallationForUpdateAsync"/>
    public Task<Domain.Platform.EdgeInstallation?> LockEdgeInstallationForUpdateAsync(
        Guid installationId, CancellationToken cancellationToken) =>
        EdgeInstallations
            .FromSqlInterpolated($"SELECT * FROM edge_installation WHERE id = {installationId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
}
