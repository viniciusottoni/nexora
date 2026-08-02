using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.AcceptOwnerInvitation;

internal sealed class AcceptOwnerInvitationCommandHandler : IRequestHandler<AcceptOwnerInvitationCommand, Result>
{
    private const string InvalidInviteMessage = "Convite inválido ou expirado.";

    private readonly IApplicationDbContext _db;
    private readonly ISecretDigester _secretDigester;
    private readonly ICredentialHasher _credentialHasher;

    public AcceptOwnerInvitationCommandHandler(
        IApplicationDbContext db,
        ISecretDigester secretDigester,
        ICredentialHasher credentialHasher)
    {
        _db = db;
        _secretDigester = secretDigester;
        _credentialHasher = credentialHasher;
    }

    public async Task<Result> Handle(AcceptOwnerInvitationCommand request, CancellationToken cancellationToken)
    {
        var secretHash = _secretDigester.Digest(request.Token);

        var invite = await _db.OwnerInvites
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.SecretHash == secretHash, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        // Mesma resposta para convite inexistente, consumido ou expirado — evita que a resposta
        // sirva de oráculo para enumerar convites válidos (mesma decisão da accept_owner_invite do TS).
        if (invite is null || invite.IsConsumed || invite.IsExpired(now))
        {
            return Result.Failure(InvalidInviteMessage, ApiErrorCodes.OwnerInviteInvalidCredentials);
        }

        invite.Consume();
        invite.User.SetPassword(_credentialHasher.Hash(request.Password));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result.Success();
    }
}
