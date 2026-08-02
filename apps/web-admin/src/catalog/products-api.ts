import {
  productImageResponseSchema,
  productListResponseSchema,
  productSchema,
  prepareProductImageUploadResponseSchema,
  type ConfirmProductImageRequest,
  type CreateProductRequest,
  type PrepareProductImageUploadRequest,
  type ProductDto,
  type ProductImageResponse,
  type ProductListResponse,
  type ReorderProductsRequest,
  type UpdateProductRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/** Formatos aceitos para foto de produto — mesma lista de `PrepareProductImageUploadCommandValidator` (back-end). */
export type ProductImageContentType =
  'image/png' | 'image/jpeg' | 'image/webp' | 'image/heic' | 'image/heif';

export class ProductsApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async list(categoryId?: string): Promise<ProductListResponse> {
    const query = categoryId ? `?categoryId=${encodeURIComponent(categoryId)}` : '';
    const response = await this.fetcher(`${this.baseUrl}/v1/catalog/products${query}`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return productListResponseSchema.parse(await response.json());
  }

  async create(input: CreateProductRequest): Promise<ProductDto> {
    return this.write('/v1/catalog/products', { method: 'POST', body: JSON.stringify(input) });
  }

  async update(id: string, input: UpdateProductRequest): Promise<ProductDto> {
    return this.write(`/v1/catalog/products/${encodeURIComponent(id)}`, {
      method: 'PATCH',
      body: JSON.stringify(input),
    });
  }

  async reorder(input: ReorderProductsRequest): Promise<void> {
    const response = await this.fetcher(`${this.baseUrl}/v1/catalog/products/reorder`, {
      method: 'PATCH',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
      body: JSON.stringify(input),
    });
    await requireSuccess(response);
  }

  async activate(id: string): Promise<ProductDto> {
    return this.write(`/v1/catalog/products/${encodeURIComponent(id)}/activate`, {
      method: 'POST',
      body: '{}',
    });
  }

  async deactivate(id: string): Promise<ProductDto> {
    return this.write(`/v1/catalog/products/${encodeURIComponent(id)}/deactivate`, {
      method: 'POST',
      body: '{}',
    });
  }

  /**
   * Upload de foto de produto de ponta a ponta: prepara a URL pré-assinada, envia o arquivo
   * direto ao object storage (nenhum byte passa pela API, US-010 §10) e confirma o
   * `MediaAsset` definitivo. `blob` já deve estar recortado na proporção fixa esperada pelo
   * cardápio (ver `image-crop-dialog.tsx`).
   */
  async uploadImage(
    productId: string,
    blob: Blob,
    contentType: ProductImageContentType,
    dimensions: { width: number; height: number },
  ): Promise<ProductImageResponse> {
    const sha256 = await sha256Hex(blob);
    const prepareRequest: PrepareProductImageUploadRequest = {
      contentType,
      bytes: blob.size,
      sha256,
    };

    const prepareResponse = await this.fetcher(
      `${this.baseUrl}/v1/catalog/products/${encodeURIComponent(productId)}/image`,
      {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(prepareRequest),
      },
    );
    await requireSuccess(prepareResponse);
    const prepared = prepareProductImageUploadResponseSchema.parse(await prepareResponse.json());

    const uploadResponse = await fetch(prepared.uploadUrl, {
      method: 'PUT',
      headers: { 'Content-Type': contentType },
      body: blob,
    });
    if (!uploadResponse.ok) {
      throw new Error('Não foi possível enviar a foto do produto.');
    }

    const confirmRequest: ConfirmProductImageRequest = {
      url: prepared.publicUrl,
      contentType,
      bytes: blob.size,
      sha256,
      width: dimensions.width,
      height: dimensions.height,
    };

    const confirmResponse = await this.fetcher(
      `${this.baseUrl}/v1/catalog/products/${encodeURIComponent(productId)}/image/confirm`,
      {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(confirmRequest),
      },
    );
    await requireSuccess(confirmResponse);
    return productImageResponseSchema.parse(await confirmResponse.json());
  }

  private async write(path: string, init: RequestInit): Promise<ProductDto> {
    const response = await this.fetcher(`${this.baseUrl}${path}`, {
      ...init,
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': crypto.randomUUID(),
        ...init.headers,
      },
    });
    await requireSuccess(response);
    return productSchema.parse(await response.json());
  }
}

async function sha256Hex(blob: Blob): Promise<string> {
  const buffer = await blob.arrayBuffer();
  const digest = await crypto.subtle.digest('SHA-256', buffer);
  return Array.from(new Uint8Array(digest))
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('');
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
