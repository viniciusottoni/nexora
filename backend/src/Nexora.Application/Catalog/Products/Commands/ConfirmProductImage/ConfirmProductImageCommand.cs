using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Products.Commands.ConfirmProductImage;

/// <summary>
/// Confirma que o upload direto (pré-assinado) da foto de um produto terminou com sucesso e
/// registra o <c>MediaAsset</c> definitivo (<c>ownerType = "PRODUCT"</c>). Porta de
/// <c>POST /v1/catalog/products/:id/image/confirm</c>.
/// </summary>
public sealed record ConfirmProductImageCommand(
    Guid ProductId,
    string Url,
    string ContentType,
    int Bytes,
    string Sha256,
    int? Width,
    int? Height) : ICommand<ProductImageResponse>;
