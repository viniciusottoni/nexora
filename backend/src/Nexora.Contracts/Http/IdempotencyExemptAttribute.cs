namespace Nexora.Contracts.Http;

/// <summary>
/// Marca uma action de controller como isenta da obrigatoriedade de <c>Idempotency-Key</c>
/// (ADR-020) — lido como metadado de endpoint pelo middleware de idempotência de
/// <c>Nexora.Api.Edge</c>/<c>Nexora.Api.Cloud</c>. Vive em <c>Nexora.Contracts</c> (não em
/// <c>Nexora.Application</c>/<c>Infrastructure</c>) só porque é um <see cref="System.Attribute"/>
/// puro sem nenhuma dependência de ASP.NET Core — pode ser referenciado por qualquer camada, e
/// os controllers de ambas as APIs já referenciam Contracts.
/// </summary>
/// <remarks>
/// Uso deliberadamente restrito (decisão registrada no relatório da tarefa de plumbing que
/// introduziu o middleware): login por senha/PIN e refresh de sessão não têm efeito colateral
/// duplicável relevante — emitir dois tokens de acesso válidos em paralelo não corrompe estado
/// nem duplica um recurso de negócio, diferente de um pedido, pagamento ou pareamento de
/// dispositivo. Backup upload (<c>PUT /v1/platform/installations/{id}/backups</c>) também é
/// isento: já garante idempotência de conteúdo pelo hash SHA-256 declarado no cabeçalho
/// (<c>UploadBackupCommandHandler</c>), e bufferizar de novo um corpo de até 1&#160;GB só para uma
/// chave genérica não paga o custo. Qualquer novo uso deste atributo deve repetir esse raciocínio
/// em comentário — não é "isento porque é anônimo" (pareamento de dispositivo é anônimo e MESMO
/// ASSIM exige a chave, porque registra um dispositivo novo).
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class IdempotencyExemptAttribute : Attribute
{
}
