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
