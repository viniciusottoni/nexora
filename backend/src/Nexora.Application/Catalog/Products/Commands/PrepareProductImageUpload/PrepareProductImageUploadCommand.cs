using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Products.Commands.PrepareProductImageUpload;

/// <summary>
/// Gera uma URL de upload pré-assinada para a foto de um produto (US-010 §10, "recorte assistido
/// de imagem no upload, com proporção fixa" — o recorte acontece no cliente antes deste passo).
/// Não registra o <c>MediaAsset</c> ainda — isso só acontece em <c>ConfirmProductImageCommand</c>,
/// depois que o upload direto ao object storage é confirmado. Porta de
/// <c>POST /v1/catalog/products/:id/image</c>.
/// </summary>
public sealed record PrepareProductImageUploadCommand(Guid ProductId, string ContentType, int Bytes, string Sha256)
    : ICommand<PrepareProductImageUploadResponse>;
