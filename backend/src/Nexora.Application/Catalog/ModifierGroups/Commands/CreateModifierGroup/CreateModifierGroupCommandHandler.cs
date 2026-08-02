using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using Nexora.Shared.Security;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Catalog.ModifierGroups.Commands.CreateModifierGroup;

internal sealed class CreateModifierGroupCommandHandler
    : IRequestHandler<CreateModifierGroupCommand, Result<ModifierGroupResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<CreateModifierGroupCommandHandler> _logger;

    public CreateModifierGroupCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        ILogger<CreateModifierGroupCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<ModifierGroupResponse>> Handle(CreateModifierGroupCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<ModifierGroupResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        // Checagem de permissão feita aqui (em vez de uma AuthorizationPolicy nomeada em
        // Program.cs) porque este módulo nasceu num worktree isolado, em paralelo com outros
        // agentes, sem poder tocar Program.cs (arquivo compartilhado/proibido pela tarefa) — ver
        // relatório da tarefa para a policy "ModifierGroupRead"/"ModifierGroupWrite" recomendada
        // para reforço em profundidade no pipeline de autorização do ASP.NET Core.
        if (!PermissionAuthorization.HasPermission(_tenantContext.Permissions, "catalog:write"))
        {
            return Result<ModifierGroupResponse>.Failure(
                "Seu perfil não tem permissão para alterar o cardápio.",
                ApiErrorCodes.AuthPermissionDenied);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var group = ModifierGroup.Create(
            tenantId,
            request.Name.Trim(),
            request.MinSelect,
            request.MaxSelect,
            request.IsRequired,
            request.SortOrder);

        _db.ModifierGroups.Add(group);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "MODIFIER_GROUP_CREATED",
            entity: "modifier_group",
            occurredAt: DateTimeOffset.UtcNow,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: group.Id));

        // SaveChangesAsync é feito pelo TransactionBehavior (commands) — estado e auditoria na
        // mesma transação. Nenhum product.updated (EVT-050) aqui: um grupo recém-criado ainda não
        // está vinculado a produto nenhum (ver LinkModifierGroupToProductCommandHandler).

        _logger.LogInformation(
            "Grupo de modificadores criado. TenantId={TenantId}, GroupId={GroupId}, Name={Name}",
            tenantId, group.Id, group.Name);

        return Result<ModifierGroupResponse>.Success(new ModifierGroupResponse(
            group.Id,
            group.Name,
            group.MinSelect,
            group.MaxSelect,
            group.IsRequired,
            group.SortOrder,
            Array.Empty<ModifierResponse>(),
            Array.Empty<Guid>()));
    }
}
