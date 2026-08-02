namespace Nexora.Infrastructure.Installations;

/// <summary>
/// Chave mestra usada para derivar o pepper de lookup de PIN por tenant — porta de
/// <c>PIN_LOOKUP_MASTER_KEY</c> (<c>prisma-installation-registration.repository.ts</c>).
/// Diferente de <c>Auth:Secrets:PinLookupPepper</c> (<c>AuthSecretsOptions</c>, usado pelo edge
/// para digest local de PIN — um valor por instalação): esta é a chave da NUVEM que gera um
/// pepper diferente por tenant, entregue uma única vez no registro da instalação
/// (<see cref="Application.Installations.Abstractions.IPinLookupPepperProvider"/>) e então
/// gravada como o <c>Auth:Secrets:PinLookupPepper</c> daquela loja específica.
/// </summary>
public sealed class PinLookupMasterKeyOptions
{
    public const string SectionName = "Installations:PinLookupMasterKey";

    public string Value { get; set; } = string.Empty;
}
