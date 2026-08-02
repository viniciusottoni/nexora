using Awaken.Application.Common.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Awaken.Infrastructure.Services;

/// <summary>
/// US-195: validates IAP consumable transactions via RevenueCat REST API.
///
/// Transaction validation queries GET /v1/subscribers/{appUserId} and checks
/// that the transaction appears under the subscriber's non_subscriptions
/// (matching either the RevenueCat transaction id or the store transaction id).
/// The purchase environment ("sandbox"/"production") comes from is_sandbox;
/// when the backend runs in Production, sandbox transactions are rejected.
///
/// Temporary provider failures (5xx, network) throw so the order stays
/// "pending" upstream; definitive negatives (subscriber/transaction not found)
/// return IsValid=false and the order is rejected.
///
/// If RevenueCat:SecretApiKey is not configured, validation returns IsValid=false
/// and the purchase is rejected. Configure the key in production secrets.
/// </summary>
public class RevenueCatValidationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    IHostEnvironment hostEnvironment,
    ILogger<RevenueCatValidationService> logger) : IRevenueCatValidationService
{
    private const string BaseUrl = "https://api.revenuecat.com/v1";

    public async Task<RevenueCatTransactionValidation> ValidateTransactionAsync(
        string transactionReference,
        string appUserId,
        CancellationToken cancellationToken = default)
    {
        var apiKey = config["RevenueCat:SecretApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning(
                "RevenueCat:SecretApiKey is not configured. " +
                "IAP transaction {TransactionReference} cannot be validated — rejecting.",
                transactionReference);
            return new RevenueCatTransactionValidation(false, null, null, null, false, null);
        }

        using var client = httpClientFactory.CreateClient("RevenueCat");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.BaseAddress = new Uri(BaseUrl);

        var response = await client.GetAsync(
            $"subscribers/{Uri.EscapeDataString(appUserId)}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning(
                "RevenueCat subscriber not found for appUserId={AppUserId}. " +
                "Rejecting IAP transaction {TransactionReference}.",
                appUserId, transactionReference);
            return new RevenueCatTransactionValidation(false, null, appUserId, null, false, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            // Falha temporária do provider → exceção para o handler manter o
            // pedido "pending" e permitir nova tentativa (US-226).
            throw new HttpRequestException(
                $"RevenueCat subscriber lookup failed with status {(int)response.StatusCode} " +
                $"for transaction validation.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!TryGetSubscriberElement(document.RootElement, out var subscriber))
        {
            logger.LogWarning(
                "RevenueCat subscriber payload missing 'subscriber' node for appUserId={AppUserId}. " +
                "Rejecting IAP transaction {TransactionReference}.",
                appUserId, transactionReference);
            return new RevenueCatTransactionValidation(false, null, appUserId, null, false, null);
        }

        var purchase = FindNonSubscriptionPurchase(subscriber, transactionReference);
        if (purchase is null)
        {
            logger.LogWarning(
                "RevenueCat transaction {TransactionReference} not found under non_subscriptions " +
                "of appUserId={AppUserId}. Rejecting.",
                transactionReference, appUserId);
            return new RevenueCatTransactionValidation(false, null, appUserId, null, false, null);
        }

        var environment = purchase.Value.IsSandbox ? "sandbox" : "production";

        // Backend em Production só credita transações de production; compras
        // sandbox (Test Store, sandbox das lojas) valem apenas em dev/staging.
        if (hostEnvironment.IsProduction() && purchase.Value.IsSandbox)
        {
            logger.LogWarning(
                "RevenueCat transaction {TransactionReference} is sandbox but backend runs in " +
                "Production. Rejecting for appUserId={AppUserId} productId={ProductId}.",
                transactionReference, appUserId, purchase.Value.ProductId);
            return new RevenueCatTransactionValidation(
                false, purchase.Value.ProductId, appUserId, purchase.Value.Store, false, environment);
        }

        logger.LogInformation(
            "RevenueCat transaction {TransactionReference} validated for appUserId={AppUserId} " +
            "productId={ProductId} environment={Environment}.",
            transactionReference, appUserId, purchase.Value.ProductId, environment);

        return new RevenueCatTransactionValidation(
            true, purchase.Value.ProductId, appUserId, purchase.Value.Store, false, environment);
    }

    /// <summary>
    /// Procura a transação nas compras avulsas (non_subscriptions) do subscriber.
    /// Formato: { "pack_100": [ { "id", "store_transaction_id", "is_sandbox", "store", ... } ] }.
    /// O SDK do app reporta o store transaction id; o webhook usa o id do RevenueCat —
    /// por isso a comparação aceita ambos.
    /// </summary>
    private static NonSubscriptionPurchase? FindNonSubscriptionPurchase(
        JsonElement subscriber, string transactionReference)
    {
        if (!subscriber.TryGetProperty("non_subscriptions", out var nonSubscriptions) ||
            nonSubscriptions.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var product in nonSubscriptions.EnumerateObject())
        {
            if (product.Value.ValueKind != JsonValueKind.Array) continue;

            foreach (var purchase in product.Value.EnumerateArray())
            {
                var id = TryGetString(purchase, "id");
                var storeTransactionId = TryGetString(purchase, "store_transaction_id");
                if (!string.Equals(id, transactionReference, StringComparison.Ordinal) &&
                    !string.Equals(storeTransactionId, transactionReference, StringComparison.Ordinal))
                {
                    continue;
                }

                var isSandbox = purchase.TryGetProperty("is_sandbox", out var sandboxNode) &&
                    sandboxNode.ValueKind == JsonValueKind.True;

                return new NonSubscriptionPurchase(
                    product.Name,
                    TryGetString(purchase, "store"),
                    isSandbox);
            }
        }

        return null;
    }

    private readonly record struct NonSubscriptionPurchase(
        string ProductId,
        string? Store,
        bool IsSandbox);

    public async Task<RevenueCatSubscriptionValidation> ValidateSubscriptionAsync(
        string appUserId,
        string? fallbackAppUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var candidates = string.IsNullOrWhiteSpace(fallbackAppUserId) ||
                         string.Equals(appUserId, fallbackAppUserId, StringComparison.Ordinal)
            ? new[] { appUserId }
            : new[] { appUserId, fallbackAppUserId };

        foreach (var candidate in candidates)
        {
            var validation = await ValidateSubscriptionForAppUserIdAsync(
                candidate,
                utcNow,
                cancellationToken);

            if (validation.IsActive)
            {
                if (!string.Equals(candidate, appUserId, StringComparison.Ordinal))
                {
                    logger.LogWarning(
                        "RevenueCat subscription resolved via fallback appUserId={FallbackAppUserId} after primary appUserId={AppUserId} was not found.",
                        candidate,
                        appUserId);
                }

                return validation;
            }
        }

        return new RevenueCatSubscriptionValidation(false, null, null, null, null);
    }

    private async Task<RevenueCatSubscriptionValidation> ValidateSubscriptionForAppUserIdAsync(
        string appUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var apiKey = config["RevenueCat:SecretApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning(
                "RevenueCat:SecretApiKey is not configured. Subscription sync for appUserId={AppUserId} cannot be validated.",
                appUserId);
            return new RevenueCatSubscriptionValidation(false, null, null, null, null);
        }

        try
        {
            using var client = httpClientFactory.CreateClient("RevenueCat");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.BaseAddress = new Uri(BaseUrl);

            var response = await client.GetAsync(
                $"subscribers/{Uri.EscapeDataString(appUserId)}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "RevenueCat subscriber not found for appUserId={AppUserId}",
                    appUserId);
                return new RevenueCatSubscriptionValidation(false, null, null, null, null);
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "RevenueCat subscriber lookup failed for appUserId={AppUserId}. status={StatusCode}",
                    appUserId,
                    (int)response.StatusCode);
                return new RevenueCatSubscriptionValidation(false, null, null, null, null);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!TryGetSubscriberElement(document.RootElement, out var subscriber))
            {
                logger.LogWarning(
                    "RevenueCat subscriber payload missing 'subscriber' node for appUserId={AppUserId}",
                    appUserId);
                return new RevenueCatSubscriptionValidation(false, null, null, null, null);
            }

            var active = FindLatestActiveEntitlement(subscriber, utcNow) ??
                FindLatestActiveSubscription(subscriber, utcNow);

            if (active is null)
            {
                logger.LogDebug(
                    "RevenueCat subscription inactive for appUserId={AppUserId}",
                    appUserId);
                return new RevenueCatSubscriptionValidation(false, null, null, null, null);
            }

            var plan = DerivePlanFromProductId(active.Value.ProductId);
            if (plan is not ("monthly" or "annual"))
            {
                logger.LogWarning(
                    "RevenueCat subscription sync found active product with unknown plan. appUserId={AppUserId} productId={ProductId}",
                    appUserId,
                    active.Value.ProductId);
                return new RevenueCatSubscriptionValidation(false, null, active.Value.Entitlement, active.Value.ProductId, active.Value.ExpiresAtUtc);
            }

            logger.LogDebug(
                "RevenueCat subscription active for appUserId={AppUserId} entitlement={Entitlement} productId={ProductId} plan={Plan} expiresAt={ExpiresAt}",
                appUserId,
                active.Value.Entitlement,
                active.Value.ProductId,
                plan,
                active.Value.ExpiresAtUtc);

            return new RevenueCatSubscriptionValidation(
                true,
                plan,
                active.Value.Entitlement,
                active.Value.ProductId,
                active.Value.ExpiresAtUtc);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating RevenueCat subscription for appUserId={AppUserId}", appUserId);
            return new RevenueCatSubscriptionValidation(false, null, null, null, null);
        }
    }

    private static ActiveRevenueCatSubscription? FindLatestActiveEntitlement(JsonElement subscriber, DateTime utcNow)
    {
        if (!subscriber.TryGetProperty("entitlements", out var entitlements) ||
            entitlements.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        ActiveRevenueCatSubscription? latest = null;
        foreach (var entitlement in entitlements.EnumerateObject())
        {
            if (!entitlement.Value.TryGetProperty("expires_date", out var expiresNode) ||
                !TryGetUtcDate(expiresNode, out var expiresAt) ||
                expiresAt <= utcNow)
            {
                continue;
            }

            var productId = TryGetString(entitlement.Value, "product_identifier");
            if (string.IsNullOrEmpty(productId)) continue;

            if (latest is null || expiresAt > latest.Value.ExpiresAtUtc)
            {
                latest = new ActiveRevenueCatSubscription(
                    entitlement.Name,
                    productId,
                    expiresAt);
            }
        }

        return latest;
    }

    private static ActiveRevenueCatSubscription? FindLatestActiveSubscription(JsonElement subscriber, DateTime utcNow)
    {
        if (!subscriber.TryGetProperty("subscriptions", out var subscriptions) ||
            subscriptions.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        ActiveRevenueCatSubscription? latest = null;
        foreach (var subscription in subscriptions.EnumerateObject())
        {
            if (!subscription.Value.TryGetProperty("expires_date", out var expiresNode) ||
                !TryGetUtcDate(expiresNode, out var expiresAt) ||
                expiresAt <= utcNow)
            {
                continue;
            }

            if (latest is null || expiresAt > latest.Value.ExpiresAtUtc)
            {
                latest = new ActiveRevenueCatSubscription(
                    null,
                    subscription.Name,
                    expiresAt);
            }
        }

        return latest;
    }

    private static bool TryGetUtcDate(JsonElement node, out DateTime utc)
    {
        utc = default;
        if (node.ValueKind != JsonValueKind.String) return false;

        var value = node.GetString();
        if (string.IsNullOrEmpty(value)) return false;

        if (!DateTimeOffset.TryParse(value, out var parsed)) return false;
        utc = parsed.UtcDateTime;
        return true;
    }

    private static bool TryGetSubscriberElement(JsonElement root, out JsonElement subscriber)
    {
        if (root.TryGetProperty("subscriber", out subscriber))
        {
            return true;
        }

        if (root.TryGetProperty("value", out var value) &&
            value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty("subscriber", out subscriber))
        {
            return true;
        }

        subscriber = default;
        return false;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var node) &&
            node.ValueKind == JsonValueKind.String
                ? node.GetString()
                : null;
    }

    private static string DerivePlanFromProductId(string? productId)
    {
        if (string.IsNullOrEmpty(productId)) return "unknown";

        if (productId.Contains("annual", StringComparison.OrdinalIgnoreCase) ||
            productId.Contains("yearly", StringComparison.OrdinalIgnoreCase))
        {
            return "annual";
        }

        if (productId.Contains("monthly", StringComparison.OrdinalIgnoreCase) ||
            productId.Contains("month", StringComparison.OrdinalIgnoreCase))
        {
            return "monthly";
        }

        return "unknown";
    }

    private readonly record struct ActiveRevenueCatSubscription(
        string? Entitlement,
        string ProductId,
        DateTime ExpiresAtUtc);
}
