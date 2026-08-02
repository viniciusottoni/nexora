using Awaken.Contracts.Users;
using MediatR;

namespace Awaken.Application.Users.Queries.GetEffectiveExperienceLevel;

public record GetEffectiveExperienceLevelQuery : IRequest<EffectiveExperienceLevelResponse>;
