using Nexora.Domain.Catalog;

namespace Nexora.Application.Catalog.Variants;

/// <summary>
/// Resolve o <see cref="Channel"/> textual recebido em contratos (US-011/US-014) — canal ausente
/// vira <see cref="Channel.DineIn"/> (padrão adotado ao criar uma variante/preço, US-011 §"O que
/// construir"). Usado por <c>CreateVariantCommandHandler</c>, <c>SetVariantPriceCommandHandler</c>,
/// <c>ListVariantsForProductQueryHandler</c> e <c>GetPublicMenuQueryHandler</c> — internal porque
/// só é chamado dentro de <c>Nexora.Application</c>.
/// </summary>
internal static class ChannelParser
{
    public static bool TryParse(string? value, out Channel channel)
    {
        if (value is null)
        {
            channel = Channel.DineIn;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out channel);
    }
}
