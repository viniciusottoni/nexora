using Nexora.Application.Abstractions.Platform;

namespace Nexora.Infrastructure.Platform;

/// <summary>
/// Implementação padrão (dev-safe) de <see cref="ICertificateIssuer"/> — mesmo idioma de
/// <c>LoggingEmailDispatcher</c>/<c>LoggingPushNotificationSender</c>: a infraestrutura real
/// (cliente ACME/DNS-01, ex.: Let's Encrypt) é um PRÓXIMO PASSO, não desta história. Este
/// repositório roda em sandbox sem posse real de domínio internet-facing — não dá para emitir um
/// certificado de verdade aqui, ao contrário da verificação DNS (<see cref="DnsClientDomainVerificationService"/>),
/// que é só leitura pública sem exigir posse de nada.
/// </summary>
/// <remarks>
/// <b>Simplificação deliberada, para deixar registrada:</b> "emite" sincronamente, sempre com
/// sucesso, com validade de ~90 dias (mesmo padrão do Let's Encrypt de verdade) — o suficiente
/// para exercitar de ponta a ponta o resto do fluxo (limiar de renovação de 15 dias em
/// <c>TenantDomain.IsCertificateExpiringSoon</c>, o alerta de falha via
/// <see cref="Nexora.Application.Abstractions.Notifications.IPlatformAlertNotifier"/>, o worker de
/// renovação) em teste/dev. Não valida DNS-01, não fala com nenhuma CA, não gera chave nem
/// certificado real. Quando a infraestrutura ACME entrar, ela troca esta implementação atrás da
/// MESMA porta <see cref="ICertificateIssuer"/> — nenhum chamador (comandos/worker) precisa mudar.
/// </remarks>
public sealed class ManualCertificateIssuer : ICertificateIssuer
{
    private static readonly TimeSpan Validity = TimeSpan.FromDays(90);

    public Task<CertificateIssuanceResult> IssueAsync(string domain, CancellationToken cancellationToken)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(new CertificateIssuanceResult(true, issuedAt, issuedAt.Add(Validity)));
    }
}
