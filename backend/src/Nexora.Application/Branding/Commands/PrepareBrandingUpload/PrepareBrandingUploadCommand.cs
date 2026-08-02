using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Branding;

namespace Nexora.Application.Branding.Commands.PrepareBrandingUpload;

/// <summary>
/// Gera uma URL de upload pré-assinada para um ativo de marca (logo/favicon/ícone PWA) e
/// registra o <c>MediaAsset</c> correspondente. Porta de <c>POST /v1/tenant/branding/logo</c>.
/// </summary>
public sealed record PrepareBrandingUploadCommand(string Kind, string ContentType, int Bytes, string Sha256)
    : ICommand<UploadBrandingAssetResponse>;
