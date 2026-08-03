using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nexora.Domain.Platform;

namespace Nexora.Infrastructure.Persistence.Interceptors;

/// <summary>
/// E-09/US-090, cenário "Correlação com o evento" — preenche <c>trace_id</c> (W3C Trace Context,
/// ADR-022) em todo <see cref="AuditLog"/>/<see cref="DomainEvent"/> novo que ainda não o informou
/// explicitamente, a partir de <see cref="Activity.Current"/>. Centralizado aqui em vez de em cada
/// um dos ~50 handlers que já chamam <c>AuditLog.Create</c>/<c>DomainEvent.Create</c> — nenhum
/// precisa mudar para ganhar correlação de traço.
/// </summary>
/// <remarks>
/// Usa <c>entry.Property(...).CurrentValue</c> (metadado do EF Core, não reflexão do CLR) para
/// escrever em uma propriedade com setter privado — a mesma técnica que o EF Core já usa para
/// hidratar essas entidades a partir do banco, então não exige expor um setter público nem
/// <c>InternalsVisibleTo</c> entre <c>Nexora.Domain</c> e <c>Nexora.Infrastructure</c>.
/// </remarks>
public sealed class AuditTraceInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTraceId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTraceId(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplyTraceId(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var traceId = Activity.Current?.TraceId.ToHexString();
        if (traceId is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<AuditLog>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TraceId is null)
            {
                entry.Property(nameof(AuditLog.TraceId)).CurrentValue = traceId;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<DomainEvent>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TraceId is null)
            {
                entry.Property(nameof(DomainEvent.TraceId)).CurrentValue = traceId;
            }
        }
    }
}
