using Awaken.Contracts.Users;
using MediatR;

namespace Awaken.Application.Users.Queries.GetOnboardingStatus;

public record GetOnboardingStatusQuery : IRequest<OnboardingStatusResponse>;
