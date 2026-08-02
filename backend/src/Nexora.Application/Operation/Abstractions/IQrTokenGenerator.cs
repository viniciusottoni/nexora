namespace Nexora.Application.Operation.Abstractions;

/// <summary>
/// Gera o <c>qr_token</c> opaco de <see cref="Nexora.Domain.Operation.DiningTable"/> (US-020 §8:
/// "gerado com entropia criptográfica e indexado — é a chave de entrada do cliente no sistema").
/// Não é apresentação (diferente de <c>IPairingCodeGenerator</c>, que gera um código de 6 dígitos
/// para um humano digitar) — é um segredo de entrada, por isso precisa de entropia alta o
/// suficiente para não ser adivinhável mesmo conhecendo o token de outra mesa (cenário Gherkin
/// "Token não adivinhável").
/// </summary>
public interface IQrTokenGenerator
{
    /// <summary>Token opaco, criptograficamente aleatório, sem relação previsível com tokens anteriores.</summary>
    string Generate();
}
