using Awaken.Contracts.Admin.Economy;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetAdminGoldLedger;

/// <summary>
/// US-229: listagem paginada admin do ledger Gold com filtros.
/// </summary>
public record GetAdminGoldLedgerQuery(
    Guid? UserId,
    string? Direction,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page,
    int PageSize)
    : IRequest<GoldLedgerPageResponse>;
