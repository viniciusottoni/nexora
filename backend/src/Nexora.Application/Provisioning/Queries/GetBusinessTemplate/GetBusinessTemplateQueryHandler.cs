using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Provisioning.Queries.GetBusinessTemplate;

internal sealed class GetBusinessTemplateQueryHandler
    : IRequestHandler<GetBusinessTemplateQuery, Result<BusinessTemplateDetailResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetBusinessTemplateQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BusinessTemplateDetailResponse>> Handle(
        GetBusinessTemplateQuery request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var template = await _db.BusinessTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

        if (template is null)
        {
            return Result<BusinessTemplateDetailResponse>.Failure(
                "Modelo de negócio não encontrado.", ApiErrorCodes.BusinessTemplateNotFound);
        }

        var response = new BusinessTemplateDetailResponse(
            template.Code,
            template.Name,
            template.Version,
            template.IsActive,
            template.ConfigJson,
            template.SeedsJson,
            template.CreatedAt,
            template.UpdatedAt);

        return Result<BusinessTemplateDetailResponse>.Success(response);
    }
}
