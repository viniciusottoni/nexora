using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using MediatR;

namespace Nexora.Application.Abstractions.Behaviors;

/// <summary>
/// Único <c>SaveChangesAsync</c> por command (ADR-006: estado + evento na mesma transação).
/// Só roda para <see cref="ICommand"/>/<see cref="ICommand{TResponse}"/> — queries nunca escrevem.
/// Se o handler já retornou falha (Result.IsFailure), não salva nada.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IApplicationDbContext _db;

    public TransactionBehavior(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var isCommand = request is ICommand || IsGenericCommand(request);
        if (!isCommand)
        {
            return await next();
        }

        var response = await next();

        if (response is Result result && result.IsFailure)
        {
            return response;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return response;
    }

    private static bool IsGenericCommand(TRequest request) =>
        request.GetType().GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
}
