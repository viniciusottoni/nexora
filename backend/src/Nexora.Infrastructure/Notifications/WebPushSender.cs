using System.Globalization;
using System.Buffers.Binary;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexora.Application.Abstractions.Notifications;

namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// <see cref="IPushNotificationSender"/> real — Web Push com criptografia de mensagem RFC 8291
/// (aes128gcm) e autenticação VAPID RFC 8292 (JWT ES256), implementados com <c>System.Security.Cryptography</c>
/// puro do BCL (P-256 já é nativo — diferente do Ed25519 usado em Installation, que precisou de
/// BouncyCastle; ver nota em <c>Directory.Packages.props</c>), sem depender de uma biblioteca
/// Web Push de terceiro. Registrado por <c>Nexora.Api.Cloud/Program.cs</c> quando
/// <c>WebPush:PublicKeyBase64Url</c> está configurado; do contrário, <see cref="LoggingPushNotificationSender"/>
/// assume o papel (mesmo padrão de <c>SmtpEmailDispatcher</c>/<c>LoggingEmailDispatcher</c>).
/// </summary>
public sealed class WebPushSender : IPushNotificationSender
{
    private const int RecordSize = 4096;
    private static readonly byte[] Info = Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0");
    private static readonly byte[] NonceInfo = Encoding.ASCII.GetBytes("Content-Encoding: nonce\0");

    private readonly WebPushOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(IOptions<WebPushOptions> options, HttpClient httpClient, ILogger<WebPushSender> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendAsync(PushTarget target, PushPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            var plaintext = JsonSerializer.SerializeToUtf8Bytes(new
            {
                title = payload.Title,
                body = payload.Body,
                severity = payload.Severity,
                alertId = payload.AlertId,
            });

            var body = Encrypt(target, plaintext);
            var vapidJwt = BuildVapidJwt(target.Endpoint);

            using var request = new HttpRequestMessage(HttpMethod.Post, target.Endpoint)
            {
                Content = new ByteArrayContent(body),
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            request.Content.Headers.ContentEncoding.Add("aes128gcm");
            request.Headers.TryAddWithoutValidation("TTL", _options.TtlSeconds.ToString(CultureInfo.InvariantCulture));
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                $"vapid t={vapidJwt}, k={_options.PublicKeyBase64Url}");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogPushFalhou(target.Endpoint, (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Assinatura expirada/inválida no navegador (404/410 do provedor) ou qualquer outra
            // falha de rede não deve derrubar o worker (DeliverPendingPushCommand processa vários
            // alertas/assinaturas por rodada) — mesma resiliência dos demais workers deste módulo.
            LogPushErro(ex, target.Endpoint);
        }
    }

    /// <summary>RFC 8291 §3.1-3.4 — um único registro (o payload de alerta é sempre pequeno, nunca se aproxima de <see cref="RecordSize"/>).</summary>
    private static byte[] Encrypt(PushTarget target, byte[] plaintext)
    {
        var userPublicKeyBytes = Base64UrlDecode(target.P256dhKey);
        var authSecret = Base64UrlDecode(target.AuthKey);

        using var ecdhUser = ECDiffieHellman.Create();
        ecdhUser.ImportSubjectPublicKeyInfo(ConvertUncompressedPointToSpki(userPublicKeyBytes), out _);

        using var ecdhLocal = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var localPublicKeyBytes = ExportUncompressedPublicKey(ecdhLocal);

        var sharedSecret = ecdhLocal.DeriveRawSecretAgreement(ecdhUser.PublicKey);

        var salt = RandomNumberGenerator.GetBytes(16);

        // ecdh_secret -> IKM (RFC 8291 §3.4): PRK_key = HMAC-SHA256(auth_secret, ecdh_secret); IKM = HKDF-Expand(PRK_key, key_info, 32).
        var keyInfo = Combine("WebPush: info\0"u8.ToArray(), userPublicKeyBytes, localPublicKeyBytes);
        var prkKey = HMACSHA256.HashData(authSecret, sharedSecret);
        var ikm = HKDF.Expand(HashAlgorithmName.SHA256, prkKey, 32, keyInfo);

        // aes128gcm content-coding (RFC 8188 §2.1): PRK = HKDF-Extract(salt, IKM); CEK/NONCE derivados dele.
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, ikm, salt);
        var contentEncryptionKey = HKDF.Expand(HashAlgorithmName.SHA256, prk, 16, Info);
        var nonce = HKDF.Expand(HashAlgorithmName.SHA256, prk, 12, NonceInfo);

        // Delimitador de último registro (0x02) antes de cifrar — RFC 8188 §2.
        var padded = new byte[plaintext.Length + 1];
        Array.Copy(plaintext, padded, plaintext.Length);
        padded[^1] = 0x02;

        var ciphertext = new byte[padded.Length];
        var tag = new byte[16];
        using (var aesGcm = new AesGcm(contentEncryptionKey, tag.Length))
        {
            aesGcm.Encrypt(nonce, padded, ciphertext, tag);
        }

        // Cabeçalho aes128gcm (RFC 8188 §2.1): salt(16) || rs(4, big-endian) || idlen(1) || keyid(65).
        var header = new byte[16 + 4 + 1 + localPublicKeyBytes.Length];
        Array.Copy(salt, 0, header, 0, 16);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(16, 4), RecordSize);
        header[20] = (byte)localPublicKeyBytes.Length;
        Array.Copy(localPublicKeyBytes, 0, header, 21, localPublicKeyBytes.Length);

        return Combine(header, ciphertext, tag);
    }

    /// <summary>RFC 8292 — JWT ES256 com <c>aud</c> = origem do endpoint, assinado com a chave privada VAPID (escalar `d`, P-256).</summary>
    private string BuildVapidJwt(string endpoint)
    {
        var audience = new Uri(endpoint).GetLeftPart(UriPartial.Authority);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds();

        var headerJson = """{"typ":"JWT","alg":"ES256"}"""u8.ToArray();
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            aud = audience,
            exp = expiresAt,
            sub = _options.Subject,
        });

        var unsigned = $"{Base64UrlEncode(headerJson)}.{Base64UrlEncode(payloadJson)}";

        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Base64UrlDecode(_options.PrivateKeyD),
            Q = ImportPointFromUncompressed(Base64UrlDecode(_options.PublicKeyBase64Url)),
        });

        var signature = ecdsa.SignData(
            Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return $"{unsigned}.{Base64UrlEncode(signature)}";
    }

    private static byte[] ExportUncompressedPublicKey(ECDiffieHellman ecdh)
    {
        var parameters = ecdh.ExportParameters(false);
        return Combine(new byte[] { 0x04 }, parameters.Q.X!, parameters.Q.Y!);
    }

    private static ECPoint ImportPointFromUncompressed(byte[] uncompressed)
    {
        var coordinateLength = (uncompressed.Length - 1) / 2;
        return new ECPoint
        {
            X = uncompressed[1..(1 + coordinateLength)],
            Y = uncompressed[(1 + coordinateLength)..],
        };
    }

    /// <summary>
    /// A chave pública do navegador (<c>p256dh</c>) chega como ponto EC não comprimido — envolve
    /// num SubjectPublicKeyInfo mínimo (DER) para <c>ECDiffieHellman.ImportSubjectPublicKeyInfo</c>
    /// aceitar; evita reimplementar parsing DER completo só para trocar o ponto.
    /// </summary>
    private static byte[] ConvertUncompressedPointToSpki(byte[] uncompressedPoint)
    {
        using var ecdh = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = ImportPointFromUncompressed(uncompressedPoint),
        });
        return ecdh.ExportSubjectPublicKeyInfo();
    }

    private static byte[] Combine(params byte[][] parts)
    {
        var result = new byte[parts.Sum(p => p.Length)];
        var offset = 0;
        foreach (var part in parts)
        {
            Array.Copy(part, 0, result, offset, part.Length);
            offset += part.Length;
        }

        return result;
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private void LogPushFalhou(string endpoint, int statusCode) =>
        _logger.LogWarning("Push rejeitado pelo provedor para {Endpoint} — status {StatusCode}.", endpoint, statusCode);

    private void LogPushErro(Exception ex, string endpoint) =>
        _logger.LogWarning(ex, "Falha ao enviar push para {Endpoint}.", endpoint);
}
