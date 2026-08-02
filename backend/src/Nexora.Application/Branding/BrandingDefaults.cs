using System.Text.Json;
using Nexora.Contracts.Branding;

namespace Nexora.Application.Branding;

/// <summary>
/// Paleta e textos padrão para um tenant recém-provisionado, cuja seção <c>branding</c> em
/// <c>TenantConfig</c> ainda é <c>"{}"</c> (o template de provisionamento não pré-popula marca —
/// ver <c>Nexora.Domain.Provisioning.ProvisioningTemplates</c>). Também serve de fallback se
/// o JSON armazenado, por qualquer motivo, não corresponder mais ao formato atual.
/// </summary>
public static class BrandingDefaults
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public static BrandingDto Create(string tenantName)
    {
        var shortName = tenantName.Length <= 12 ? tenantName : tenantName[..12];

        return new BrandingDto(
            new BrandingColorsDto("#1E3446", "#B8965A", "#F7F2E8", "#FFFFFF"),
            new BrandingLogoDto(null, null),
            Favicon: null,
            new BrandingFontsDto("Inter", "EB Garamond"),
            Radius: 8,
            new BrandingTextsDto(string.Empty, string.Empty, string.Empty, string.Empty),
            new BrandingPwaDto(tenantName, shortName, "#1E3446", Array.Empty<BrandingPwaIconDto>()));
    }

    public static BrandingDto Parse(string brandingJson, string tenantName)
    {
        if (string.IsNullOrWhiteSpace(brandingJson) || brandingJson.Trim() == "{}")
            return Create(tenantName);

        try
        {
            var parsed = JsonSerializer.Deserialize<BrandingDto>(brandingJson, SerializerOptions);

            // Deserialização por construtor posicional (record) não falha com campo aninhado
            // ausente — só preenche null. Se o JSON persistido não bate mais com o formato
            // atual (migração de schema, dado corrompido), cai para o padrão em vez de deixar
            // um BrandingDto parcialmente nulo estourar NullReferenceException adiante.
            if (parsed is null || parsed.Colors is null || parsed.Logo is null ||
                parsed.Fonts is null || parsed.Texts is null || parsed.Pwa is null)
            {
                return Create(tenantName);
            }

            return parsed;
        }
        catch (JsonException)
        {
            return Create(tenantName);
        }
    }
}
