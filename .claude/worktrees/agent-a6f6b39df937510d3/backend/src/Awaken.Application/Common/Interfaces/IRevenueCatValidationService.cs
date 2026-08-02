namespace Awaken.Application.Common.Interfaces;

/// <summary>
/// US-195: server-side validation of IAP transactions via RevenueCat.
/// Items must only be granted after this validation passes — never from
/// the transaction ID alone supplied by the app.
/// </summary>
/// US-226 (RN-005): <see cref="Environment"/> identifica se a transação foi
/// validada em ambiente "sandbox" ou "production" no provider — populado a
/// partir do campo is_sandbox retornado pelo RevenueCat. Em ambiente de
/// Production do backend, transações sandbox são rejeitadas (IsValid=false).
public record RevenueCatTransactionValidation(
    bool IsValid,
    string? ProductId,
    string? AppUserId,
    string? Store,
    bool IsConsumed,
    string? Environment = null);

public record RevenueCatSubscriptionValidation(
    bool IsActive,
    string? Plan,
    string? Entitlement,
    string? ProductId,
    DateTime? ExpiresAtUtc);

public interface IRevenueCatValidationService
{
    /// <param name="transactionReference">
    /// ID da transação retornado pelo SDK do RevenueCat no app.
    /// </param>
    /// <param name="appUserId">
    /// App User ID no RevenueCat — o app faz Purchases.logIn com o ID do
    /// usuário do backend, então este é o UserId em formato string.
    /// </param>
    Task<RevenueCatTransactionValidation> ValidateTransactionAsync(
        string transactionReference,
        string appUserId,
        CancellationToken cancellationToken = default);

    Task<RevenueCatSubscriptionValidation> ValidateSubscriptionAsync(
        string appUserId,
        string? fallbackAppUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
