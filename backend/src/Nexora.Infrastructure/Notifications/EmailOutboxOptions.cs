namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// Configuração do envelope cifrado gravado em <c>email_outbox</c> (ver <see cref="EmailOutboxSender"/>)
/// e do worker de entrega (<see cref="EmailOutboxDeliveryWorker"/>).
/// </summary>
public sealed class EmailOutboxOptions
{
    public const string SectionName = "EmailOutbox";

    /// <summary>
    /// Segredo usado para derivar a chave AES-256-GCM do envelope (SHA-256 do valor configurado,
    /// mesmo esquema de <c>INVITATION_ENCRYPTION_KEY</c> do NestJS original). Nunca comitar o
    /// valor real — vem de User Secrets/variável de ambiente/segredo do orquestrador.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>Intervalo entre iterações do worker de entrega, em segundos (padrão 20s).</summary>
    public int PollingIntervalSeconds { get; set; } = 20;

    /// <summary>Máximo de registros pendentes processados por tenant em cada iteração.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Tentativas antes de marcar definitivamente como <c>FAILED</c> (sem novo <c>NextAttemptAt</c>).</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Atraso, em segundos, antes da próxima tentativa depois de uma falha de entrega.</summary>
    public int RetryDelaySeconds { get; set; } = 60;

    /// <summary>
    /// SMTP do dispatcher real. <see cref="SmtpEmailOptions.Host"/> vazio (padrão, ex.: ambiente de
    /// desenvolvimento/CI sem servidor de e-mail configurado) faz o DI registrar
    /// <see cref="LoggingEmailDispatcher"/> em vez de <see cref="SmtpEmailDispatcher"/> — ver
    /// registro condicional em <c>Nexora.Api.Cloud/Program.cs</c>.
    /// </summary>
    public SmtpEmailOptions Smtp { get; set; } = new();
}

/// <summary>Credenciais/endpoint SMTP usados por <see cref="SmtpEmailDispatcher"/>.</summary>
public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@nexora.app";
    public string FromName { get; set; } = "Nexora";
}
