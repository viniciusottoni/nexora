using Awaken.Contracts.Economy;
using MediatR;

namespace Awaken.Application.Economy.Queries.GetWallet;

/// US-186: consulta o saldo da carteira de Gold do usuario autenticado,
/// criando a carteira (saldo 0) de forma preguicosa no primeiro acesso
/// (CA-001).
public record GetWalletQuery : IRequest<WalletResponse>;
