using Awaken.Contracts.Admin.Reports;
using MediatR;

namespace Awaken.Application.Admin.Reports.Queries.GetOperationalReport;

/// <summary>
/// US-170: consulta de relatório operacional consolidado para o site admin.
/// Período padrão: últimos 7 dias até hoje.
/// </summary>
public record GetOperationalReportQuery(
    DateTime? From,
    DateTime? To,
    string? Environment) : IRequest<OperationalReportResponse>;
