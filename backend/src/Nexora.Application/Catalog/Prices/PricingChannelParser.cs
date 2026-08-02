using Nexora.Domain.Catalog;

namespace Nexora.Application.Catalog.Prices;

/// <summary>
/// Resolve o <see cref="Channel"/> textual recebido nos contratos deste módulo (US-014). Nome
/// deliberadamente distinto de um eventual <c>ChannelParser</c> de US-011 (que não existia neste
/// worktree no momento em que este arquivo foi escrito — ver nota em
/// <c>Nexora.Shared.Errors.ApiErrorCodes.Pricing</c>) para não colidir por nome de tipo quando as
/// duas histórias forem mescladas. Canal ausente é inválido aqui — diferente de US-011 (onde
/// ausência vira <c>DineIn</c> por ser o padrão de "preço base"), os endpoints desta US sempre
/// operam sobre um canal explícito (a tabela inteira é resolvida de uma vez em
/// <c>ListVariantPricesByChannelQuery</c>, que não recebe canal nenhum).
/// </summary>
internal static class PricingChannelParser
{
    public static bool TryParse(string? value, out Channel channel)
    {
        if (value is null)
        {
            channel = default;
            return false;
        }

        return Enum.TryParse(value, ignoreCase: true, out channel);
    }
}
