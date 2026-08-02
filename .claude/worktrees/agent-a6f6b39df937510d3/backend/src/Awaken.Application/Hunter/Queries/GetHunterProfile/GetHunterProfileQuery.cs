using Awaken.Contracts.Hunter;
using MediatR;

namespace Awaken.Application.Hunter.Queries.GetHunterProfile;

public record GetHunterProfileQuery : IRequest<HunterProfileResponse>;
