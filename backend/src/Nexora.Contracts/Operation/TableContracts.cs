namespace Nexora.Contracts.Operation;

/// <summary>
/// Porta de <c>tableSchema</c> (<c>packages/contracts/src/operation-tables.ts</c>). Nunca carrega
/// <c>qr_token</c> — o token é um segredo de entrada (US-020 §8), só sai do servidor embutido na
/// imagem do QR Code do PDF de exportação, nunca em JSON puro.
/// </summary>
public sealed record TableResponse(
    Guid Id,
    Guid AreaId,
    string AreaName,
    string Label,
    short Seats,
    string Status,
    bool Active,
    short SortOrder);

/// <summary>Porta de <c>tableListResponseSchema</c>. <c>GET /v1/tables?areaId=...</c>.</summary>
public sealed record TableListResponse(IReadOnlyList<TableResponse> Items);

/// <summary>Porta de <c>createTableRequestSchema</c>. <c>POST /v1/tables</c>.</summary>
public sealed record CreateTableRequest(Guid AreaId, string Label, short Seats);

/// <summary>
/// Porta de <c>createTablesBulkRequestSchema</c>. <c>POST /v1/tables/bulk</c> — cenário Gherkin
/// "Criação em lote": rótulos sequenciais <c>From</c>..<c>To</c> (ex.: mesas "1" a "20").
/// </summary>
public sealed record CreateTablesBulkRequest(Guid AreaId, int From, int To, short Seats);

/// <summary>Porta de <c>tablesBulkResponseSchema</c>.</summary>
public sealed record TablesBulkResponse(IReadOnlyList<TableResponse> Items);

/// <summary>Porta de <c>updateTableRequestSchema</c>. <c>PATCH /v1/tables/{id}</c>.</summary>
public sealed record UpdateTableRequest(Guid AreaId, string Label, short Seats, short SortOrder);
