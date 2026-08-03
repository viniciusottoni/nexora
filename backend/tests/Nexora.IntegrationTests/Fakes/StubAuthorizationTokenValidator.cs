using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Security;
using Nexora.Shared.Errors;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo mínimo de <see cref="IAuthorizationTokenValidator"/> para containers de teste que
/// PRECISAM resolver a dependência (constructor injection de
/// <c>RequestBillCommandHandler</c>/<c>RequestBillByQrCommandHandler</c>/
/// <c>RegisterPartialPaymentCommandHandler</c>, US-035) mas cujos cenários nunca de fato exercitam
/// elevação pontual (nenhum item pendente, ou modo WARN/IGNORE — a checagem BLOCK nunca chega a
/// chamar <see cref="ValidateAsync"/>). Sempre nega, para nunca mascarar silenciosamente um teste
/// que passe a depender de autorização de verdade sem trocar para a pilha real (ver
/// <c>PendingItemsOnCloseIntegrationTests</c>/<c>CancelOrderIntegrationTests</c>, que usam a
/// implementação de produção).
/// </summary>
public sealed class StubAuthorizationTokenValidator : IAuthorizationTokenValidator
{
    public Task<Result<AuthorizationGrant>> ValidateAsync(string? token, string requiredAction, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<AuthorizationGrant>.Failure("Autorização não disponível neste teste.", ApiErrorCodes.AuthorizationRequired));
}
