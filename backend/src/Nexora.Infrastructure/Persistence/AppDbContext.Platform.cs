using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

public partial class AppDbContext
{
    public DbSet<Domain.Platform.Tenant> Tenants => Set<Domain.Platform.Tenant>();
    public DbSet<Domain.Platform.TenantConfig> TenantConfigs => Set<Domain.Platform.TenantConfig>();
    public DbSet<Domain.Platform.Store> Stores => Set<Domain.Platform.Store>();
    public DbSet<Domain.Platform.Station> Stations => Set<Domain.Platform.Station>();
    public DbSet<Domain.Platform.EdgeInstallation> EdgeInstallations => Set<Domain.Platform.EdgeInstallation>();
    public DbSet<Domain.Platform.AppUser> Users => Set<Domain.Platform.AppUser>();
    public DbSet<Domain.Platform.OwnerInvite> OwnerInvites => Set<Domain.Platform.OwnerInvite>();
    public DbSet<Domain.Platform.EmailOutbox> EmailOutboxes => Set<Domain.Platform.EmailOutbox>();
    public DbSet<Domain.Platform.Role> Roles => Set<Domain.Platform.Role>();
    public DbSet<Domain.Platform.UserRole> UserRoles => Set<Domain.Platform.UserRole>();
    public DbSet<Domain.Platform.Device> Devices => Set<Domain.Platform.Device>();
    public DbSet<Domain.Platform.AuthAttempt> AuthAttempts => Set<Domain.Platform.AuthAttempt>();
    public DbSet<Domain.Platform.AuthSession> AuthSessions => Set<Domain.Platform.AuthSession>();
    public DbSet<Domain.Platform.InstallationNonce> InstallationNonces => Set<Domain.Platform.InstallationNonce>();
    public DbSet<Domain.Platform.PairingCode> PairingCodes => Set<Domain.Platform.PairingCode>();
    public DbSet<Domain.Platform.AuditLog> AuditLogs => Set<Domain.Platform.AuditLog>();

    /// <inheritdoc cref="Application.Abstractions.Persistence.IApplicationDbContext.AuditLogsWithMinAmount"/>
    public IQueryable<Domain.Platform.AuditLog> AuditLogsWithMinAmount(decimal minAmount) =>
        AuditLogs.FromSqlInterpolated(
            $"""
            SELECT * FROM audit_log
            WHERE COALESCE(
                (after->>'discount')::numeric,
                (after->>'amount')::numeric,
                (after->>'newAmount')::numeric,
                (after->>'total')::numeric
            ) >= {minAmount}
            """);
    public DbSet<Domain.Platform.IdempotencyKey> IdempotencyKeys => Set<Domain.Platform.IdempotencyKey>();
    public DbSet<Domain.Platform.DomainEvent> DomainEvents => Set<Domain.Platform.DomainEvent>();
    public DbSet<Domain.Platform.MediaAsset> MediaAssets => Set<Domain.Platform.MediaAsset>();
    public DbSet<Domain.Platform.TenantSecret> TenantSecrets => Set<Domain.Platform.TenantSecret>();
    public DbSet<Domain.Platform.PushSubscription> PushSubscriptions => Set<Domain.Platform.PushSubscription>();

    // Plataforma em escala — E-14
    public DbSet<Domain.Platform.InstallationIncident> InstallationIncidents => Set<Domain.Platform.InstallationIncident>();
    public DbSet<Domain.Platform.TenantDomain> TenantDomains => Set<Domain.Platform.TenantDomain>();
    public DbSet<Domain.Platform.SupportAccess> SupportAccesses => Set<Domain.Platform.SupportAccess>();
    public DbSet<Domain.Platform.Release> Releases => Set<Domain.Platform.Release>();
    public DbSet<Domain.Platform.OnboardingStep> OnboardingSteps => Set<Domain.Platform.OnboardingStep>();
    public DbSet<Domain.Provisioning.BusinessTemplate> BusinessTemplates => Set<Domain.Provisioning.BusinessTemplate>();
}
