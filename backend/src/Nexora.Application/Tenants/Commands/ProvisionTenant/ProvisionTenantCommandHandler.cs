using System.Security.Cryptography;
using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Common;
using Nexora.Domain.Finance;
using Nexora.Domain.Platform;
using Nexora.Domain.Provisioning;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Tenants.Commands.ProvisionTenant;

internal sealed class ProvisionTenantCommandHandler
    : IRequestHandler<ProvisionTenantCommand, Result<ProvisionTenantResponse>>
{
    private const string EdgeInstallationTokenTemplate = "owner-invite";
    private static readonly TimeSpan InstallTokenTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan OwnerInviteTtl = TimeSpan.FromHours(72);

    private readonly IApplicationDbContext _db;
    private readonly ISecretDigester _secretDigester;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ProvisionTenantCommandHandler> _logger;

    public ProvisionTenantCommandHandler(
        IApplicationDbContext db,
        ISecretDigester secretDigester,
        IEmailSender emailSender,
        ILogger<ProvisionTenantCommandHandler> logger)
    {
        _db = db;
        _secretDigester = secretDigester;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<ProvisionTenantResponse>> Handle(
        ProvisionTenantCommand request,
        CancellationToken cancellationToken)
    {
        var slugTaken = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Slug == request.Slug && t.DeletedAt == null, cancellationToken);

        if (slugTaken)
        {
            return Result<ProvisionTenantResponse>.Failure(
                "Este endereço já está em uso.",
                ApiErrorCodes.TenantSlugAlreadyTaken,
                new Dictionary<string, string[]> { ["slug"] = new[] { "Este endereço já está em uso." } });
        }

        var template = ProvisioningTemplates.Get(request.Template);
        var now = DateTimeOffset.UtcNow;

        var tenant = Tenant.Create(request.Slug, request.Name, timezone: request.StoreTimezone);
        _db.Tenants.Add(tenant);

        // RLS (ADR-004): tenant_config/store/role/station/app_user/... têm a política
        // tenant_isolation (WITH CHECK (tenant_id = current_tenant_id())). O ator desta rota é um
        // admin de plataforma sem tenant próprio — nenhum token hoje emite a claim "tid" para
        // PlatformAdmin (ver PENDÊNCIA em Program.cs) — então ICurrentTenantContext.TenantId é nulo
        // e current_tenant_id() nunca teria um valor igual ao tenant_id sendo inserido. Mesmo
        // mecanismo de LoginWithPasswordCommandHandler (SetTenantContextAsync): sessão, não
        // SET LOCAL, para sobreviver aos vários comandos SQL emitidos até o SaveChangesAsync do
        // TransactionBehavior (ver nota em AppDbContext.Auth.cs). O "tenant" em si é a única tabela
        // sem RLS (raiz global), por isso este SET só é necessário a partir daqui.
        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var tenantConfig = TenantConfig.CreateWithConfig(
            tenant.Id,
            brandingJson: JsonSerializer.Serialize(template.Config.Branding),
            operationJson: JsonSerializer.Serialize(template.Config.Operation),
            thresholdsJson: JsonSerializer.Serialize(template.Config.Thresholds),
            modulesJson: JsonSerializer.Serialize(template.Config.Modules),
            fiscalJson: JsonSerializer.Serialize(template.Config.Fiscal),
            printersJson: JsonSerializer.Serialize(template.Config.Printers),
            paymentsJson: JsonSerializer.Serialize(template.Config.Payments),
            maintenanceJson: JsonSerializer.Serialize(template.Config.Maintenance));
        _db.TenantConfigs.Add(tenantConfig);

        var store = Store.Create(tenant.Id, request.StoreName, isDefault: true, timezone: request.StoreTimezone);
        _db.Stores.Add(store);

        Guid? ownerRoleId = null;
        var roleEvents = new List<(Guid RoleId, IReadOnlyList<string> Permissions)>();
        foreach (var roleTemplate in template.Roles)
        {
            var role = Role.Create(tenant.Id, roleTemplate.Code, roleTemplate.Name, isSystem: true);
            role.UpdatePermissions(JsonSerializer.Serialize(roleTemplate.Permissions));
            _db.Roles.Add(role);

            if (roleTemplate.Code == "OWNER")
                ownerRoleId = role.Id;

            roleEvents.Add((role.Id, roleTemplate.Permissions));
        }

        if (ownerRoleId is null)
            throw new DomainException("O template de provisionamento precisa definir o papel OWNER.");

        short sortOrder = 0;
        foreach (var stationTemplate in template.Stations)
        {
            sortOrder++;
            var station = Station.Create(
                tenant.Id,
                store.Id,
                code: stationTemplate.Type.ToString().ToUpperInvariant(),
                name: stationTemplate.Name,
                type: stationTemplate.Type,
                sortOrder: stationTemplate.SortOrder);
            station.UpdateCapacity(stationTemplate.CapacitySlots, stationTemplate.AvgCookSeconds);

            // US-017 §4 (cenário "Praças padrão do modelo pizzaria"): o forno é o gargalo por
            // definição de negócio (RF-CAT-09) — cada template decide sua própria praça-gargalo
            // aqui, nunca por condicional de tenant (ADR-013).
            if (stationTemplate.Type == StationType.Oven)
            {
                station.MarkAsBottleneck();
            }

            _db.Stations.Add(station);
        }

        // Docs/Domain/12-Seeds-e-Dados-Iniciais.md §5/§6 (passos 7 e 8 do provisionamento automático,
        // RF-PLT-05) — categorias de despesa e contas financeiras padrão do modelo de negócio.
        foreach (var expenseCategoryTemplate in template.ExpenseCategories)
        {
            var expenseCategory = ExpenseCategory.Create(
                tenant.Id, expenseCategoryTemplate.Name, expenseCategoryTemplate.Group, expenseCategoryTemplate.IsCmv);
            _db.ExpenseCategories.Add(expenseCategory);
        }

        foreach (var financialAccountTemplate in template.FinancialAccounts)
        {
            var financialAccount = FinancialAccount.Create(
                tenant.Id, financialAccountTemplate.Name, financialAccountTemplate.Type);
            _db.FinancialAccounts.Add(financialAccount);
        }

        var owner = AppUser.Invite(tenant.Id, request.OwnerName, request.OwnerEmail);
        _db.Users.Add(owner);

        var ownerUserRole = UserRole.Create(tenant.Id, owner.Id, ownerRoleId.Value, storeId: null);
        _db.UserRoles.Add(ownerUserRole);

        var installTokenRaw = CreateRawSecret();
        var installTokenHash = _secretDigester.Digest(installTokenRaw);
        var edgeInstallation = EdgeInstallation.Create(tenant.Id, store.Id, label: $"Servidor local — {store.Name}");
        edgeInstallation.IssueInstallToken(installTokenHash, now.Add(InstallTokenTtl));
        _db.EdgeInstallations.Add(edgeInstallation);

        var inviteSecretRaw = CreateRawSecret();
        var inviteSecretHash = _secretDigester.Digest(inviteSecretRaw);
        var ownerInvite = OwnerInvite.Create(tenant.Id, owner.Id, request.OwnerEmail, inviteSecretHash, now.Add(OwnerInviteTtl));
        _db.OwnerInvites.Add(ownerInvite);

        await _emailSender.EnqueueAsync(
            tenant.Id,
            request.OwnerEmail,
            EdgeInstallationTokenTemplate,
            new Dictionary<string, string>
            {
                ["token"] = inviteSecretRaw,
                ["tenantName"] = tenant.Name,
                ["ownerName"] = owner.Name
            },
            cancellationToken);

        _db.DomainEvents.Add(DomainEvent.Create(
            tenant.Id,
            type: "tenant.config_updated",
            aggregateType: "tenant",
            aggregateId: tenant.Id,
            payload: JsonSerializer.Serialize(new { configVersion = 1, template = template.Code }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: store.Id));

        foreach (var (roleId, permissions) in roleEvents)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenant.Id,
                type: "permission.changed",
                aggregateType: "role",
                aggregateId: roleId,
                payload: JsonSerializer.Serialize(new { roleId, permissions }),
                origin: "CLOUD",
                occurredAt: now,
                storeId: store.Id));
        }

        // SaveChangesAsync é feito pelo TransactionBehavior (command). Uma corrida rara em que
        // dois pedidos provisionam o mesmo slug ao mesmo tempo só é pega pela constraint única
        // do banco nesse ponto — não vira um 422 amigável, vira 500; mesma limitação já aceita
        // em outros handlers deste pipeline (a checagem de unicidade acima cobre o caso comum).

        _logger.LogInformation(
            "Tenant provisionado. TenantId={TenantId}, Slug={Slug}, Template={Template}",
            tenant.Id, tenant.Slug, template.Code);

        var response = new ProvisionTenantResponse(
            new ProvisionedTenantResponse(tenant.Id, tenant.Slug, tenant.Status.ToString()),
            new ProvisionedStoreResponse(store.Id, store.Name),
            installTokenRaw,
            $"./install.sh --tenant={tenant.Id} --token={installTokenRaw}",
            request.OwnerEmail,
            ProvisioningChecklist.Create()
                .Select(item => new ProvisioningChecklistItemResponse(item.Code, item.Label, item.Status))
                .ToList());

        return Result<ProvisionTenantResponse>.Success(response);
    }

    private static string CreateRawSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
