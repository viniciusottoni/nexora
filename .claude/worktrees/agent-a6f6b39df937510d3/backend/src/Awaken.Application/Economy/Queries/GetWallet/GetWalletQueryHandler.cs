using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Economy;
using MediatR;

namespace Awaken.Application.Economy.Queries.GetWallet;

public class GetWalletQueryHandler(
    IGoldWalletService walletService,
    ICurrentUserService currentUserService) : IRequestHandler<GetWalletQuery, WalletResponse>
{
    public async Task<WalletResponse> Handle(GetWalletQuery request, CancellationToken cancellationToken)
    {
        var wallet = await walletService.GetOrCreateWalletAsync(currentUserService.UserId, cancellationToken);
        return new WalletResponse(wallet.Balance);
    }
}
