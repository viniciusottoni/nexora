namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// Renderização mínima de assunto/corpo a partir do código de template gravado em
/// <c>email_outbox.template</c> e das variáveis decifradas do envelope — não é um motor de
/// template completo (Handlebars/Razor etc.), só substituição literal de <c>{chave}</c>, suficiente
/// para os poucos templates transacionais deste módulo (ADR-013: o CONTEÚDO do e-mail é o mesmo
/// para todo tenant; só as variáveis mudam — texto fixo, nunca branch por cliente).
/// </summary>
internal static class EmailTemplateRenderer
{
    public const string OwnerInvite = "owner-invite";

    /// <summary>
    /// US-145 §10 "Notificação ao cliente no momento da concessão, não depois" — enfileirado por
    /// <c>RecordSupportAccessCommandHandler</c> para todo <c>AppUser</c> com papel OWNER do
    /// tenant alvo. Conteúdo fixo (ADR-013) — só as variáveis mudam por concessão.
    /// </summary>
    public const string SupportAccessGranted = "support-access-granted";

    public static (string Subject, string Body) Render(string template, IReadOnlyDictionary<string, string> variables)
    {
        var (subjectTemplate, bodyTemplate) = template switch
        {
            OwnerInvite => (
                "Bem-vindo(a) à Nexora, {ownerName}!",
                "Olá {ownerName},\n\n" +
                "O estabelecimento \"{tenantName}\" foi criado na Nexora e você foi convidado(a) " +
                "como proprietário(a).\n\n" +
                "Use o código de convite abaixo para definir sua senha de acesso (válido por 72 horas):\n\n" +
                "{token}\n\n" +
                "Se você não esperava este e-mail, ignore-o."),
            SupportAccessGranted => (
                "Acesso de suporte concedido em {tenantName}",
                "Olá,\n\n" +
                "A equipe de suporte da Nexora solicitou e recebeu acesso temporário aos dados de " +
                "\"{tenantName}\".\n\n" +
                "Motivo informado: {reason}\n" +
                "Duração concedida: {durationMinutes} minutos\n" +
                "Expira em: {expiresAt}\n\n" +
                "Você pode consultar e revogar este acesso a qualquer momento no histórico de acessos " +
                "de suporte do seu painel."),
            _ => (
                "Nexora — {template}",
                string.Join('\n', variables.Select(kv => $"{kv.Key}: {kv.Value}"))),
        };

        var mergedVariables = template == OwnerInvite
            ? variables
            : MergeTemplateCode(variables, template);

        return (Interpolate(subjectTemplate, mergedVariables), Interpolate(bodyTemplate, mergedVariables));
    }

    private static IReadOnlyDictionary<string, string> MergeTemplateCode(
        IReadOnlyDictionary<string, string> variables, string template)
    {
        var merged = new Dictionary<string, string>(variables, StringComparer.Ordinal) { ["template"] = template };
        return merged;
    }

    private static string Interpolate(string template, IReadOnlyDictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
        {
            template = template.Replace("{" + key + "}", value, StringComparison.Ordinal);
        }

        return template;
    }
}
