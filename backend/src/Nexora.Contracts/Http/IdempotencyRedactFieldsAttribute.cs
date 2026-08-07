namespace Nexora.Contracts.Http;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — resposta do doc. de spec §15
/// ("[PENDÊNCIA] decidir antes da implementação" o que fazer com o segredo bruto quando a mesma
/// <c>Idempotency-Key</c> é repetida, ADR-020 guarda a resposta original por 24h e o middleware a
/// devolve verbatim). Marca uma action cuja resposta de SUCESSO carrega um segredo mostrado uma
/// única vez (ex.: token bruto de instalação) — o middleware de idempotência
/// (<c>Nexora.Api.Cloud</c>/<c>Nexora.Api.Edge</c>) continua devolvendo o corpo INTEGRAL para a
/// chamada que de fato executou a ação (é o próprio ato de emitir/mostrar o segredo), mas GRAVA
/// para reenvio uma cópia com os campos aqui nomeados trocados por <c>null</c> — assim, se a MESMA
/// chave for repetida dentro da janela de 24h, a resposta reenviada nunca torna a expor o valor
/// bruto (decisão registrada no relatório da US-156: "a rotação aconteceu e a intenção foi
/// atendida, mas o segredo, por definição de exibição única, não volta a aparecer").
/// </summary>
/// <remarks>
/// Mecanismo aditivo e opt-in: nenhuma action sem este atributo muda de comportamento — o middleware
/// só verifica a presença do metadado quando vai decidir o que PERSISTIR, nunca o que devolve ao
/// vivo na primeira chamada.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotencyRedactFieldsAttribute : Attribute
{
    public IdempotencyRedactFieldsAttribute(params string[] fieldNames)
    {
        FieldNames = fieldNames;
    }

    /// <summary>Nomes de propriedade JSON (nível raiz, camelCase — como serializado pelo `System.Text.Json` padrão do ASP.NET Core) a substituir por <c>null</c> na cópia armazenada.</summary>
    public IReadOnlyList<string> FieldNames { get; }
}
