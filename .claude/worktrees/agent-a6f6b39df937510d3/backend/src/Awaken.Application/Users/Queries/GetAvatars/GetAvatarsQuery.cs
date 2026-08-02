using Awaken.Contracts.Users;
using MediatR;

namespace Awaken.Application.Users.Queries.GetAvatars;

/// US-234: lista o catalogo de avatares internos com estado de bloqueio/selecao
/// para o usuario atual (RN-003/RN-005).
public record GetAvatarsQuery : IRequest<IReadOnlyList<AvatarCatalogItemResponse>>;
