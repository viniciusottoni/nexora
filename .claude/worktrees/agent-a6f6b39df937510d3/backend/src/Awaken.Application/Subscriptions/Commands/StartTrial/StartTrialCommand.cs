using Awaken.Contracts.Subscriptions;
using MediatR;

namespace Awaken.Application.Subscriptions.Commands.StartTrial;

public record StartTrialCommand : IRequest<StartTrialResponse>;
