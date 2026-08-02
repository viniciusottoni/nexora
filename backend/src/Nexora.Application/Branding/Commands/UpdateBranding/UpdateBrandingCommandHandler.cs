using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Branding;
using Nexora.Contracts.Branding;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Branding.Commands.UpdateBranding;

internal sealed class UpdateBrandingCommandHandler : IRequestHandler<UpdateBrandingCommand, Result<UpdateBrandingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateBrandingCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<UpdateBrandingResponse>> Handle(UpdateBrandingCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<UpdateBrandingResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;

        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        var tenantConfig = await _db.TenantConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);

        if (tenant is null || tenantConfig is null)
        {
            return Result<UpdateBrandingResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.NotFound);
        }

        var current = BrandingDefaults.Parse(tenantConfig.Branding, tenant.Name);
        var merged = Merge(current, request.Patch);
        var changedKeys = FlattenChangedKeys(request.Patch);

        tenantConfig.UpdateBranding(JsonSerializer.Serialize(merged));

        var occurredAt = DateTimeOffset.UtcNow;
        var brandingKeys = changedKeys.Where(k => !k.StartsWith("texts.", StringComparison.Ordinal)).ToList();
        var textKeys = changedKeys.Where(k => k.StartsWith("texts.", StringComparison.Ordinal)).ToList();

        if (brandingKeys.Count > 0)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "tenant.branding_updated",
                aggregateType: "tenant",
                aggregateId: tenantId,
                payload: JsonSerializer.Serialize(new { changedKeys = brandingKeys, configVersion = tenantConfig.BrandingVersion }),
                origin: "CLOUD",
                occurredAt: occurredAt,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        if (textKeys.Count > 0)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "tenant.config_updated",
                aggregateType: "tenant",
                aggregateId: tenantId,
                payload: JsonSerializer.Serialize(new { changedKeys = textKeys, configVersion = tenantConfig.BrandingVersion }),
                origin: "CLOUD",
                occurredAt: occurredAt,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        var contrast = BrandingContrast.Validate(merged.Colors.Primary, merged.Colors.Surface, merged.Colors.OnPrimary);

        // SaveChangesAsync é feito pelo TransactionBehavior.

        var response = new UpdateBrandingResponse(
            new TenantBrandingInfoResponse(tenant.Id, tenant.Name),
            merged,
            tenantConfig.BrandingVersion,
            new BrandingContrastResponse(
                contrast.Valid,
                contrast.MinimumRatio,
                contrast.Issues.Select(i => new BrandingContrastIssueResponse(i.Pair, i.Ratio, i.Suggested)).ToList()));

        return Result<UpdateBrandingResponse>.Success(response);
    }

    private static BrandingDto Merge(BrandingDto current, UpdateBrandingRequest patch)
    {
        var colors = new BrandingColorsDto(
            patch.Colors?.Primary ?? current.Colors.Primary,
            patch.Colors?.Secondary ?? current.Colors.Secondary,
            patch.Colors?.Surface ?? current.Colors.Surface,
            patch.Colors?.OnPrimary ?? current.Colors.OnPrimary);

        var logo = new BrandingLogoDto(
            patch.Logo?.Light ?? current.Logo.Light,
            patch.Logo?.Dark ?? current.Logo.Dark);

        var fonts = new BrandingFontsDto(
            patch.Fonts?.Body ?? current.Fonts.Body,
            patch.Fonts?.Display ?? current.Fonts.Display);

        var texts = new BrandingTextsDto(
            patch.Texts?.Welcome ?? current.Texts.Welcome,
            patch.Texts?.OrderConfirmed ?? current.Texts.OrderConfirmed,
            patch.Texts?.Thanks ?? current.Texts.Thanks,
            patch.Texts?.Terms ?? current.Texts.Terms);

        var pwa = new BrandingPwaDto(
            patch.Pwa?.Name ?? current.Pwa.Name,
            patch.Pwa?.ShortName ?? current.Pwa.ShortName,
            patch.Pwa?.ThemeColor ?? current.Pwa.ThemeColor,
            patch.Pwa?.Icons ?? current.Pwa.Icons);

        return new BrandingDto(
            colors,
            logo,
            patch.Favicon ?? current.Favicon,
            fonts,
            patch.Radius ?? current.Radius,
            texts,
            pwa);
    }

    private static List<string> FlattenChangedKeys(UpdateBrandingRequest patch)
    {
        var keys = new List<string>();

        void AddNested(string section, params (string Field, object? Value)[] fields)
        {
            foreach (var (field, value) in fields)
            {
                if (value is not null)
                    keys.Add($"{section}.{field}");
            }
        }

        if (patch.Colors is not null)
        {
            AddNested(
                "colors",
                ("primary", patch.Colors.Primary),
                ("secondary", patch.Colors.Secondary),
                ("surface", patch.Colors.Surface),
                ("onPrimary", patch.Colors.OnPrimary));
        }

        if (patch.Logo is not null)
        {
            AddNested("logo", ("light", patch.Logo.Light), ("dark", patch.Logo.Dark));
        }

        if (patch.Favicon is not null)
        {
            keys.Add("favicon");
        }

        if (patch.Fonts is not null)
        {
            AddNested("fonts", ("body", patch.Fonts.Body), ("display", patch.Fonts.Display));
        }

        if (patch.Radius is not null)
        {
            keys.Add("radius");
        }

        if (patch.Texts is not null)
        {
            AddNested(
                "texts",
                ("welcome", patch.Texts.Welcome),
                ("orderConfirmed", patch.Texts.OrderConfirmed),
                ("thanks", patch.Texts.Thanks),
                ("terms", patch.Texts.Terms));
        }

        if (patch.Pwa is not null)
        {
            AddNested(
                "pwa",
                ("name", patch.Pwa.Name),
                ("shortName", patch.Pwa.ShortName),
                ("themeColor", patch.Pwa.ThemeColor),
                ("icons", patch.Pwa.Icons));
        }

        return keys;
    }
}
