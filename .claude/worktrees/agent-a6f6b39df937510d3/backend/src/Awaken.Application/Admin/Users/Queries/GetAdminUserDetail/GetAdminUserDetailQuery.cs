using Awaken.Contracts.Admin.Users;
using MediatR;

namespace Awaken.Application.Admin.Users.Queries.GetAdminUserDetail;

/// <summary>
/// US-167: consulta administrativa de detalhe de um usuário específico.
/// </summary>
public record GetAdminUserDetailQuery(Guid UserId) : IRequest<AdminUserDetailResponse>;
