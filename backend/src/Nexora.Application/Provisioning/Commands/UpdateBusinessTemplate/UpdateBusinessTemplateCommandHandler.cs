using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Provisioning.Commands.UpdateBusinessTemplate;

internal sealed class UpdateBusinessTemplateCommandHandler
    : IRequestHandler<UpdateBusinessTemplateCommand, Result<BusinessTemplateDetailResponse>>
{
    private readonly IApplicationDbContext _db;

    public UpdateBusinessTemplateCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BusinessTemplateDetailResponse>> Handle(
        UpdateBusinessTemplateCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var template = await _db.BusinessTemplates
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

        if (template is null)
        {
            return Result<BusinessTemplateDetailResponse>.Failure(
                "Modelo de negócio não encontrado.", ApiErrorCodes.BusinessTemplateNotFound);
        }

        // US-142 §4 "Atualização de modelo": incrementa Version. Tenants já provisionados
        // guardaram template_code/template_version no momento em que foram criados
        // (TenantConfig.CreateWithConfig) — nenhuma linha aqui os alcança, então continuam com a
        // configuração antiga mesmo depois deste Update (cenário "tenants existentes não devem
        // ser alterados").
        template.Update(request.Name, request.ConfigJson, request.SeedsJson);

        // SaveChangesAsync é feito pelo TransactionBehavior.

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
