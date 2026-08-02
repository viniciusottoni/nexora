using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.ExportGoldEconomyAlertsCsv;

/// <summary>
/// US-228 seção 5/12: exportação CSV dos alertas de economia Gold (RN-006: nunca inclui
/// saldo, payload de pagamento ou dado de provider — apenas tipo/severidade/status/usuário
/// afetado/ambiente/timestamps). Segue o mesmo padrão de ExportOperationalReportCsvQuery.
/// </summary>
public record ExportGoldEconomyAlertsCsvQuery(
    string? Severity,
    string? Status) : IRequest<byte[]>;
