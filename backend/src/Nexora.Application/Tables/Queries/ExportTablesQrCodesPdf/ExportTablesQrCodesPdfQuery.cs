using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Tables.Queries.ExportTablesQrCodesPdf;

/// <summary>
/// Cenário Gherkin "Exportação para impressão": gera um PDF com um QR Code por página,
/// identificado pelo rótulo da mesa (US-020 §4/§10). Porta de
/// <c>GET /v1/tables/qr-codes.pdf?areaId=...</c> — <c>AreaId</c> nulo exporta todas as mesas
/// ativas do tenant.
/// </summary>
public sealed record ExportTablesQrCodesPdfQuery(Guid? AreaId) : IQuery<TablesQrCodesPdfResult>;

/// <summary>Bytes do PDF pronto para download — não é um DTO JSON, por isso fica fora de <c>Nexora.Contracts</c>.</summary>
public sealed record TablesQrCodesPdfResult(byte[] Content, string FileName);
