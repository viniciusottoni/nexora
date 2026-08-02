using Awaken.Contracts.Users;
using MediatR;

namespace Awaken.Application.Users.Queries.GetUserProfile;

public record GetUserProfileQuery : IRequest<UserProfileResponse>;
