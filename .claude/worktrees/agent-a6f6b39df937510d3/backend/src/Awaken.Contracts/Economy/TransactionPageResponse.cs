namespace Awaken.Contracts.Economy;

/// <summary>
/// US-192: resposta paginada do extrato de transacoes do usuario.
/// </summary>
public record TransactionPageResponse(
    IReadOnlyList<TransactionItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore);
