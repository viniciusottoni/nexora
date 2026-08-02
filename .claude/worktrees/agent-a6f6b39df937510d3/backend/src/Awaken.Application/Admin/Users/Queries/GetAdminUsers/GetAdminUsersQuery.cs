using Awaken.Contracts.Admin.Users;
using MediatR;

namespace Awaken.Application.Admin.Users.Queries.GetAdminUsers;

/// <summary>
/// US-167: consulta administrativa paginada de usuários com filtro por busca, plano e status.
/// </summary>
public record GetAdminUsersQuery(
    string? Search,
    string? Plan,
    string? Status,
    int Page,
    int PageSize) : IRequest<AdminUserListResponse>;
