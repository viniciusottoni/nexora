using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// <see cref="IEmailDispatcher"/> real, via <c>System.Net.Mail.SmtpClient</c> (BCL — evita puxar
/// mais um pacote de terceiro só para isto). Registrado por <c>Nexora.Api.Cloud/Program.cs</c>
/// quando <c>EmailOutbox:Smtp:Host</c> está configurado; do contrário, <see cref="LoggingEmailDispatcher"/>
/// assume o papel. Qualquer provedor SMTP compatível serve (Postmark/SES/SendGrid via SMTP relay,
/// Mailhog em dev) — não há acoplamento a um provedor específico.
/// </summary>
public sealed class SmtpEmailDispatcher : IEmailDispatcher
{
    private readonly EmailOutboxOptions _options;

    public SmtpEmailDispatcher(IOptions<EmailOutboxOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(
        string recipient,
        string template,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken)
    {
        var smtp = _options.Smtp;
        var (subject, body) = EmailTemplateRenderer.Render(template, variables);

        using var message = new MailMessage
        {
            From = new MailAddress(smtp.FromAddress, smtp.FromName),
            Subject = subject,
            Body = body,
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
        };

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
