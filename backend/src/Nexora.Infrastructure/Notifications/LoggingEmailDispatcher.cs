using Microsoft.Extensions.Logging;

namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// <see cref="IEmailDispatcher"/> de fallback para ambiente sem SMTP configurado (dev/CI/teste) —
/// registrado por <c>Nexora.Api.Cloud/Program.cs</c> quando <c>EmailOutbox:Smtp:Host</c> está vazio.
/// Não é um "TODO": <see cref="EmailOutboxDeliveryWorker"/> ainda assim decifra o envelope, chama
/// este dispatcher e marca o registro como <c>SENT</c> — o mecanismo de entrega existe e é
/// exercitado de ponta a ponta, só a entrega real por SMTP é substituída por log estruturado.
/// </summary>
public sealed class LoggingEmailDispatcher : IEmailDispatcher
{
    private readonly ILogger<LoggingEmailDispatcher> _logger;

    public LoggingEmailDispatcher(ILogger<LoggingEmailDispatcher> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string recipient,
        string template,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken)
    {
        var (subject, body) = EmailTemplateRenderer.Render(template, variables);

        _logger.LogInformation(
            "E-mail não enviado por SMTP real (EmailOutbox:Smtp:Host não configurado) — Destinatario={Recipient} " +
            "Template={Template} Assunto={Subject}\n{Body}",
            recipient, template, subject, body);

        return Task.CompletedTask;
    }
}
