using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Operation.Abstractions;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Queries.ExportTablesQrCodesPdf;

internal sealed class ExportTablesQrCodesPdfQueryHandler : IRequestHandler<ExportTablesQrCodesPdfQuery, Result<TablesQrCodesPdfResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IQrCodePdfRenderer _renderer;

    public ExportTablesQrCodesPdfQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IQrCodePdfRenderer renderer)
    {
        _db = db;
        _tenantContext = tenantContext;
        _renderer = renderer;
    }

    public async Task<Result<TablesQrCodesPdfResult>> Handle(ExportTablesQrCodesPdfQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<TablesQrCodesPdfResult>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var fileSuffix = "todas-as-mesas";

        if (request.AreaId is { } areaId)
        {
            var area = await _db.Areas.AsNoTracking().SingleOrDefaultAsync(
                a => a.Id == areaId && a.TenantId == tenantId && a.DeletedAt == null, cancellationToken);
            if (area is null)
            {
                return Result<TablesQrCodesPdfResult>.Failure("Ambiente não encontrado.", ApiErrorCodes.AreaNotFound);
            }

            fileSuffix = Slugify(area.Name);
        }

        var query = _db.DiningTables
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.DeletedAt == null && t.IsActive);

        if (request.AreaId is { } filterAreaId)
        {
            query = query.Where(t => t.AreaId == filterAreaId);
        }

        var rows = await query
            .OrderBy(t => t.Area.SortOrder).ThenBy(t => t.SortOrder).ThenBy(t => t.Label)
            .Select(t => new { t.Id, t.Label, AreaName = t.Area.Name, t.QrToken })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Result<TablesQrCodesPdfResult>.Failure(
                "Nenhuma mesa ativa encontrada para exportar.", ApiErrorCodes.TablesExportEmpty);
        }

        var items = rows
            .Select(r => new TableQrCodePrintItem(r.Id, r.Label, r.AreaName, r.QrToken))
            .ToList();

        var pdfBytes = _renderer.Render(items);
        var fileName = $"qr-codes-mesas-{fileSuffix}.pdf";

        return Result<TablesQrCodesPdfResult>.Success(new TablesQrCodesPdfResult(pdfBytes, fileName));
    }

    private static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-') is { Length: > 0 } trimmed ? trimmed : "area";
    }
}
