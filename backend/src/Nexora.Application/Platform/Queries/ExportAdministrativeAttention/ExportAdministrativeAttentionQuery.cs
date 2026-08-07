using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Platform.Support;

namespace Nexora.Application.Platform.Queries.ExportAdministrativeAttention;

/// <summary>US-157 §"Exportação auditável de metadados administrativos" — <c>GET /v1/platform/attention/export</c>. Mesmo formato de bytes-prontos-para-download de <c>ExportTablesQrCodesPdfQuery</c> (US-020), mas CSV em vez de PDF (não há precedente de exportação estruturada no backend a reaproveitar — ver relatório da tarefa).</summary>
public sealed record ExportAdministrativeAttentionQuery(
    IReadOnlyCollection<AttentionSeverity> Severity,
    Guid? ActorId) : ICommand<AdministrativeAttentionExportResult>;

/// <summary>Bytes do CSV prontos para download — não é um DTO JSON, por isso fica fora de <c>Nexora.Contracts</c> (mesma decisão de <c>TablesQrCodesPdfResult</c>).</summary>
public sealed record AdministrativeAttentionExportResult(byte[] Content, string FileName);
