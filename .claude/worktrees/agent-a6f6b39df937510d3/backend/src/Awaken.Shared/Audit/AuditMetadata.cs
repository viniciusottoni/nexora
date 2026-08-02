using System.Text.Json;

namespace Awaken.Shared.Audit;

public static class AuditMetadata
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "token", "password", "secret", "key", "receipt", "card", "cvv", "pan",
        "transactionData", "receiptData", "paymentMethod"
    };

    /// <summary>
    /// Serializes <paramref name="data"/> to a compact JSON string, excluding any property
    /// whose name matches a known-sensitive key. Suitable for audit log metadata.
    /// Never throws — returns "{}" if serialization fails.
    /// </summary>
    public static string Safe(object data)
    {
        try
        {
            var dict = data.GetType()
                .GetProperties()
                .Where(p => !SensitiveKeys.Contains(p.Name))
                .ToDictionary(p => p.Name, p => p.GetValue(data));

            return JsonSerializer.Serialize(dict, Options);
        }
        catch
        {
            return "{}";
        }
    }
}
