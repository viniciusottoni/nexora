namespace Nexora.Infrastructure.Installation;

/// <summary>
/// Assina requisições HTTP de saída do edge para a nuvem — porta de
/// <c>installationRequestHeaders</c> (<c>installation-request-auth.ts</c>). Puramente técnico
/// (sem regra de negócio, sem acesso a banco), por isso vive só em Infrastructure — nenhum
/// Command precisa despachá-lo via <c>ISender</c> (diferente da verificação do lado nuvem, que
/// precisa consumir o nonce no banco — ver <c>AuthenticateInstallationRequestCommand</c>).
/// </summary>
public interface IInstallationRequestSigner
{
    /// <summary>Monta os quatro headers <c>X-Installation-*</c> para uma requisição <paramref name="method"/> <paramref name="requestUri"/>.</summary>
    Task<IReadOnlyDictionary<string, string>> SignAsync(string method, Uri requestUri, CancellationToken cancellationToken);
}
