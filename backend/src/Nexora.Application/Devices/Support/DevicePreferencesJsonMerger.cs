using System.Text.Json.Nodes;

namespace Nexora.Application.Devices.Support;

/// <summary>
/// Mescla em profundidade o "patch" de preferências (ex.: <c>{"kds":{"sound":{...}}}</c>, US-045)
/// sobre o JSON já gravado no dispositivo — necessário porque US-042 (<c>kds.stationIds</c>),
/// US-045 (<c>kds.sound</c>) e US-047 (<c>kds.peakMode</c>) escrevem sub-chaves DIFERENTES do
/// MESMO objeto <c>kds</c> em momentos diferentes; uma substituição rasa (o objeto <c>kds</c>
/// inteiro sobrescrito) apagaria a chave gravada pela feature anterior. Objetos são mesclados
/// recursivamente; array e escalar são substituídos por inteiro (é o que se espera de, por
/// exemplo, <c>kds.stationIds: [...]</c>).
/// </summary>
internal static class DevicePreferencesJsonMerger
{
    public static string Merge(string? existingJson, string patchJson)
    {
        var target = ParseObject(existingJson);
        var patch = ParseObject(patchJson);
        MergeInto(target, patch);
        return target.ToJsonString();
    }

    private static JsonObject ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
    }

    private static void MergeInto(JsonObject target, JsonObject patch)
    {
        foreach (var (key, value) in patch)
        {
            if (value is JsonObject patchChild && target[key] is JsonObject targetChild)
            {
                MergeInto(targetChild, patchChild);
            }
            else
            {
                target[key] = value?.DeepClone();
            }
        }
    }
}
