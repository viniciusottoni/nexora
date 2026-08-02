using System.Text.Json;

namespace Awaken.Infrastructure.ItemEffects;

/// <summary>
/// US-230: parse best-effort do PayloadJson de UseItemContext — usado por
/// handlers que precisam de um parâmetro específico do item (novo nickname,
/// classe-alvo). Retorna null em vez de lançar em JSON malformado; a
/// validação de conteúdo é responsabilidade de cada handler.
/// </summary>
internal static class ItemEffectPayload
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static T? Parse<T>(string? payloadJson) where T : class
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(payloadJson, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
