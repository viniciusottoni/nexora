using Awaken.Contracts.Admin.Economy;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetAdminGoldWalletDetail;

/// <summary>
/// US-229: detalhe seguro de carteira Gold de um usuário, com ledger recente.
/// </summary>
public record GetAdminGoldWalletDetailQuery(Guid UserId)
    : IRequest<GoldWalletAdminResponse?>;
