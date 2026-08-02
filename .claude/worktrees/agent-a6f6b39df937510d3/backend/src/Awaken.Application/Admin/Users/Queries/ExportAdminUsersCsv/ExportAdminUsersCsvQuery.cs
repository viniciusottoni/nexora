using MediatR;

namespace Awaken.Application.Admin.Users.Queries.ExportAdminUsersCsv;

/// <summary>
/// US-167: exportação CSV da lista de usuários para o site admin.
/// RN-002: sem senhas, tokens ou dados físicos no export.
/// </summary>
public record ExportAdminUsersCsvQuery(
    string? Search,
    string? Plan,
    string? Status) : IRequest<byte[]>;
