using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Provisioning.Queries.ListBusinessTemplates;

internal sealed class ListBusinessTemplatesQueryHandler
    : IRequestHandler<ListBusinessTemplatesQuery, Result<BusinessTemplateListResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListBusinessTemplatesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BusinessTemplateListResponse>> Handle(
        ListBusinessTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _db.BusinessTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new BusinessTemplateSummaryResponse(t.Code, t.Name, t.Version))
            .ToListAsync(cancellationToken);

        return Result<BusinessTemplateListResponse>.Success(new BusinessTemplateListResponse(templates));
    }
}
