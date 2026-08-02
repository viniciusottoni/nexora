using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nexora.Application.Auth.Shared;

/// <summary>
/// Hash SHA-256 determinístico do contexto de uma autorização pontual — porta de
/// hashAuthorizationContext/stableStringify (apps/api-edge/src/modules/auth/sensitive-authorization.service.ts).
/// Chaves de objeto são ordenadas antes de serializar, para que o mesmo contexto produza sempre o
/// mesmo hash independentemente da ordem de inserção (ADR-023: o token de elevação é vinculado ao
/// contexto exato da ação).
/// </summary>
internal static class AuthorizationContextHasher
{
    public static string Hash(IReadOnlyDictionary<string, object?> context)
    {
        var node = JsonSerializer.SerializeToNode(context) as JsonObject ?? new JsonObject();
        var canonical = Canonicalize(node)!.ToJsonString();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static JsonNode? Canonicalize(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var sorted = new JsonObject();
            foreach (var key in obj.Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal))
            {
                sorted[key] = Canonicalize(obj[key]?.DeepClone());
            }
            return sorted;
        }

        if (node is JsonArray array)
        {
            var result = new JsonArray();
            foreach (var item in array)
            {
                result.Add(Canonicalize(item?.DeepClone()));
            }
            return result;
        }

        return node?.DeepClone();
    }
}
