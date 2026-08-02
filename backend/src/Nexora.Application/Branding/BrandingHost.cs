namespace Nexora.Application.Branding;

/// <summary>Normalização e validação de host (query <c>?host=</c>) — porta de <c>normalizeHost</c> do NestJS original.</summary>
public static class BrandingHost
{
    public static bool IsValid(string? rawHost)
    {
        if (string.IsNullOrWhiteSpace(rawHost))
            return false;

        var value = rawHost.Trim();
        return !value.Any(char.IsWhiteSpace) && !value.Contains('/') && !value.Contains('?') && !value.Contains('#');
    }

    public static string Normalize(string rawHost) => rawHost.Trim().ToLowerInvariant();
}
