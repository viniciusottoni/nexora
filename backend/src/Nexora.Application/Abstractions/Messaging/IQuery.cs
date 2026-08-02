using MediatR;

namespace Nexora.Application.Abstractions.Messaging;

/// <summary>Query — nunca passa por TransactionBehavior (só leitura, AsNoTracking no handler).</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
