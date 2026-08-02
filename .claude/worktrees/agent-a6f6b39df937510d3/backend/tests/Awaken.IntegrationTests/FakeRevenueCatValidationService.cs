using Awaken.Application.Common.Interfaces;

namespace Awaken.IntegrationTests;

/// <summary>
/// Substitui o RevenueCatValidationService real nos testes de integração.
/// Desde que a validação de transação faz uma chamada HTTP real ao RevenueCat
/// (GET /v1/subscribers/{appUserId}), os testes de integração — que exercitam
/// idempotência, crédito de Gold e trilha de pedido, não o provider — precisam
/// de um fake que aprove toda transação em ambiente sandbox.
/// </summary>
public class FakeRevenueCatValidationService : IRevenueCatValidationService
{
    public Task<RevenueCatTransactionValidation> ValidateTransactionAsync(
        string transactionReference,
        string appUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RevenueCatTransactionValidation(
            true, null, appUserId, "google_play", false, "sandbox"));

    public Task<RevenueCatSubscriptionValidation> ValidateSubscriptionAsync(
        string appUserId,
        string? fallbackAppUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RevenueCatSubscriptionValidation(false, null, null, null, null));
}
