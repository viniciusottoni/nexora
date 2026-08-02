using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexora.Contracts.Catalog;

/// <summary>
/// Opção dentro de um grupo de modificadores (ex.: "Borda Catupiry", "Sem cebola") — porta de
/// <c>modifierSchema</c> (US-012, futuro <c>packages/contracts/src/catalog-modifiers.ts</c>).
/// </summary>
public sealed record ModifierResponse(
    Guid Id,
    Guid GroupId,
    string Name,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal PriceDelta,
    Guid? IngredientId,
    [property: JsonConverter(typeof(NullableMoneyJsonConverter))] decimal? Quantity,
    bool IsAvailable,
    short SortOrder);

/// <summary>
/// Serializa <c>decimal</c> como string no JSON (ADR-017: "double/float são proibidos para
/// dinheiro" — string evita perda de precisão de ponto flutuante no cliente). Aplicado só aos
/// campos de dinheiro/quantidade desta US porque ainda não existe um conversor global equivalente
/// registrado em <c>Program.cs</c> (ver <c>Nexora.Contracts.Installations.InitialSyncPageResponse</c>,
/// que já assume esse conversor global mas nenhum Program.cs dos dois projetos de Api o registra
/// hoje — gap pré-existente, fora do escopo desta tarefa tocar Program.cs). Quando esse conversor
/// global existir, estes atributos podem ser removidos sem quebrar o contrato (mesmo formato de
/// string).
/// </summary>
public sealed class MoneyJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return decimal.Parse(reader.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        return reader.GetDecimal();
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}

/// <summary>Variante nullable de <see cref="MoneyJsonConverter"/> — usada em <c>Modifier.Quantity</c> (insumo opcional).</summary>
public sealed class NullableMoneyJsonConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            return string.IsNullOrEmpty(raw) ? null : decimal.Parse(raw, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        return reader.GetDecimal();
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
    }
}
