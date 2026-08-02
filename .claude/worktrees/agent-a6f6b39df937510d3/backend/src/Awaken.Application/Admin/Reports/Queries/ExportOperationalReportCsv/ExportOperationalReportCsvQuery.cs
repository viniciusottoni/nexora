using MediatR;

namespace Awaken.Application.Admin.Reports.Queries.ExportOperationalReportCsv;

/// <summary>
/// US-170: exportação CSV do relatório operacional para o site admin.
/// </summary>
public record ExportOperationalReportCsvQuery(
    DateTime? From,
    DateTime? To,
    string? Environment) : IRequest<byte[]>;
