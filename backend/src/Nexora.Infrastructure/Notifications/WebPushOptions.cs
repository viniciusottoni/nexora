namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// Par de chaves VAPID (RFC 8292, P-256) e assunto de contato — gerado uma vez por ambiente
/// (fora desta solution, ex.: <c>openssl ecparam</c> ou biblioteca `web-push generate-vapid-keys`)
/// e injetado por configuração/segredo, nunca versionado. <see cref="PrivateKeyD"/> é o escalar `d`
/// da chave privada em base64url (formato bruto usado pela maioria das bibliotecas Web Push);
/// <see cref="PublicKeyBase64Url"/> é o ponto público não comprimido (65 bytes) em base64url, o
/// MESMO valor entregue ao navegador na assinatura (<c>PushManager.subscribe({ applicationServerKey })</c>).
/// </summary>
public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    public string PublicKeyBase64Url { get; set; } = string.Empty;
    public string PrivateKeyD { get; set; } = string.Empty;

    /// <summary>RFC 8292 exige um contato — mailto: ou https:.</summary>
    public string Subject { get; set; } = "mailto:suporte@replaystudio.app";

    public int TtlSeconds { get; set; } = 60 * 60 * 4;
}
