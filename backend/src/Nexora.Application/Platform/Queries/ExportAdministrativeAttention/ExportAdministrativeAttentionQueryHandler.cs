using System.Globalization;
using System.Text;
using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Platform.Queries.GetAttentionQueue;
using Nexora.Application.Platform.Support;
using Nexora.Domain.Platform;
using MediatR;

namespace Nexora.Application.Platform.Queries.ExportAdministrativeAttention;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — reaproveita
/// <see cref="GetAttentionQueueQuery"/> (mesmo pipeline MediatR, mesmo padrão de composição de
/// <c>GetOnboardingStatusQueryHandler</c>/<c>ActivateTenantCommandHandler</c>, que também injetam
/// <see cref="ISender"/> para compor sobre outro handler em vez de duplicar a lógica de agregação)
/// para não duplicar a projeção cross-tenant, registra a exportação no audit_log de cada tenant
/// incluído e formata o resultado como CSV. Teto de 500 itens por
/// exportação (§"Fora do escopo" não define um limite; escolha conservadora [HIPÓTESE] para não
/// prender a requisição indefinidamente caso o parque cresça muito).
/// </summary>
internal sealed class ExportAdministrativeAttentionQueryHandler
    : IRequestHandler<ExportAdministrativeAttentionQuery, Result<AdministrativeAttentionExportResult>>
{
    private const int ExportLimit = 500;
    private const int PageLimit = 100;

    private readonly ISender _sender;

    public ExportAdministrativeAttentionQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<Result<AdministrativeAttentionExportResult>> Handle(
        ExportAdministrativeAttentionQuery request, CancellationToken cancellationToken)
    {
        var items = new List<Nexora.Contracts.Platform.AttentionQueueItemResponse>();
        string? cursor = null;

        do
        {
            var queueResult = await _sender.Send(
                new GetAttentionQueueQuery(request.Severity, PageLimit, cursor), cancellationToken);

            if (queueResult.IsFailure)
                return Result<AdministrativeAttentionExportResult>.Failure(queueResult.Error!, queueResult.Code);

            items.AddRange(queueResult.Value!.Data.Take(ExportLimit - items.Count));
            cursor = items.Count < ExportLimit ? queueResult.Value.NextCursor : null;
        }
        while (cursor is not null);

        foreach (var tenantGroup in items.GroupBy(item => item.TenantId))
        {
            var auditResult = await _sender.Send(
                new RecordAdministrativeAttentionExportCommand(
                    tenantGroup.Key,
                    request.ActorId,
                    tenantGroup.Count(),
                    request.Severity.Select(value => value.ToWireLabel()).ToArray()),
                cancellationToken);

            if (auditResult.IsFailure)
                return Result<AdministrativeAttentionExportResult>.Failure(auditResult.Error!, auditResult.Code);
        }

        var builder = new StringBuilder();
        builder.AppendLine("item_id,tenant_id,tenant_name,type,severity,since,reason");

        foreach (var item in items)
        {
            builder.AppendLine(string.Join(',',
                CsvField(item.Id),
                CsvField(item.TenantId.ToString()),
                CsvField(item.TenantName),
                CsvField(item.Type),
                CsvField(item.Severity),
                CsvField(item.Since.ToString("O", CultureInfo.InvariantCulture)),
                CsvField(item.Reason)));
        }

        var fileName = $"central-de-atencao-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";
        var content = Encoding.UTF8.GetBytes(builder.ToString());
        return Result<AdministrativeAttentionExportResult>.Success(new AdministrativeAttentionExportResult(content, fileName));
    }

    /// <summary>RFC 4180 mínimo — aspas duplas ao redor de qualquer campo com vírgula/aspas/quebra de linha (o motivo é texto livre, pode conter vírgula).</summary>
    private static string CsvField(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}

internal sealed record RecordAdministrativeAttentionExportCommand(
    Guid TenantId,
    Guid? ActorId,
    int ExportedItems,
    IReadOnlyCollection<string> Severity) : ICommand;

internal sealed class RecordAdministrativeAttentionExportCommandHandler
    : IRequestHandler<RecordAdministrativeAttentionExportCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public RecordAdministrativeAttentionExportCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(
        RecordAdministrativeAttentionExportCommand request,
        CancellationToken cancellationToken)
    {
        await _db.SetTenantContextAsync(request.TenantId, cancellationToken);
        _db.AuditLogs.Add(AuditLog.Create(
            request.TenantId,
            action: "ADMINISTRATIVE_ATTENTION_EXPORTED",
            entity: "administrative_attention",
            occurredAt: DateTimeOffset.UtcNow,
            actorId: request.ActorId,
            entityId: request.TenantId,
            after: JsonSerializer.Serialize(new
            {
                exportedItems = request.ExportedItems,
                severity = request.Severity,
                format = "CSV",
            }),
            reason: "Exportação de metadados administrativos da central de atenção"));

        return Result.Success();
    }
}
