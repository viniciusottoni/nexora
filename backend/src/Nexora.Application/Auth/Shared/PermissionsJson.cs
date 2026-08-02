using System.Text.Json;

namespace Nexora.Application.Auth.Shared;

/// <summary>Leitura de <c>Role.Permissions</c> (JSONB livre, ex.: <c>["*", "order:read"]</c>).</summary>
internal static class PermissionsJson
{
    public static IReadOnlyList<string> Parse(string permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(permissionsJson) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
