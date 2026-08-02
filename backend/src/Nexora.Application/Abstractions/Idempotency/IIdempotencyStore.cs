namespace Nexora.Application.Abstractions.Idempotency;

/// <summary>
/// Porta de armazenamento da idempotência de escrita (ADR-020) — implementada em
/// <c>Nexora.Infrastructure</c> sobre <c>IApplicationDbContext</c>. Consumida pelo
/// <c>IdempotencyMiddleware</c> de <c>Nexora.Api.Edge</c>/<c>Nexora.Api.Cloud</c> (o middleware em
/// si só pode viver nas APIs — precisa de <c>HttpContext</c>/<c>RequestDelegate</c>, que são
/// ASP.NET Core, proibido em Infrastructure por ADR-039; só a porta e sua implementação sobre EF
/// Core vivem fora da API, seguindo o mesmo padrão de <c>IApplicationDbContext</c>).
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>Busca um registro existente pela chave (PK global — ver nota em <c>IdempotencyKey</c>).</summary>
    Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Tenta reservar a chave para a primeira execução (INSERT com <c>status=IN_PROGRESS</c>).
    /// Retorna <see cref="IdempotencyBeginOutcome.Started"/> quando a reserva foi bem-sucedida —
    /// o caller deve processar a requisição e depois chamar <see cref="CompleteAsync"/>.
    /// <see cref="IdempotencyBeginOutcome.AlreadyReserved"/> sinaliza uma corrida (duas
    /// requisições concorrentes com a mesma chave, ADR-020 "chave em processamento" -> 409)
    /// detectada pela violação da PK no INSERT — não é possível prevenir com um SELECT antes,
    /// por isso o método é a própria tentativa atômica.
    /// </summary>
    Task<IdempotencyBeginOutcome> BeginAsync(
        string key,
        Guid? tenantId,
        string endpoint,
        string requestHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>Grava a resposta original e marca a chave como concluída (ADR-020, 2ª chamada devolve isto).</summary>
    Task CompleteAsync(string key, int responseStatus, string? responseBody, CancellationToken cancellationToken);

    /// <summary>
    /// Remove a reserva feita por <see cref="BeginAsync"/> sem completá-la — ADR-020 "requisição
    /// original falhou com 5xx (ou exceção não tratada) não armazena, permite nova tentativa
    /// real". Sem isto, uma falha de servidor deixaria a chave presa em <c>IN_PROGRESS</c> para
    /// sempre, bloqueando o cliente de tentar de novo com a mesma chave.
    /// </summary>
    Task DiscardAsync(string key, CancellationToken cancellationToken);
}

/// <summary>Projeção somente-leitura de <c>Nexora.Domain.Platform.IdempotencyKey</c> para o middleware.</summary>
public sealed record IdempotencyRecord(
    string Endpoint,
    string RequestHash,
    string Status,
    int? ResponseStatus,
    string? ResponseBody,
    DateTimeOffset ExpiresAt)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsCompleted => Status == "COMPLETED";

    /// <summary>
    /// Reserva ainda sendo processada — só este estado justifica 409 (ADR-020, "chave em
    /// processamento"). <c>FAILED</c> (ver <see cref="IIdempotencyStore.DiscardAsync"/>) NUNCA é
    /// "em processamento", mesmo dentro da janela de <see cref="ExpiresAt"/> original — uma
    /// requisição que falhou libera a chave para retry imediato, sem esperar 24h.
    /// </summary>
    public bool IsInProgress => Status == "IN_PROGRESS";
}

public enum IdempotencyBeginOutcome
{
    Started,
    AlreadyReserved,
}
