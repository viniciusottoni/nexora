using Awaken.Contracts.Subscriptions;
using MediatR;

namespace Awaken.Application.Subscriptions.Queries.GetSubscriptionStatus;

public record GetSubscriptionStatusQuery : IRequest<SubscriptionStatusResponse>;
