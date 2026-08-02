import { z } from 'zod';

/**
 * US-010 (Cadastrar categorias e produtos) — porta de `Nexora.Contracts.Catalog.Product*`
 * (backend/src/Nexora.Contracts/Catalog/ProductRequests.cs, ProductResponses.cs,
 * PublicMenuResponses.cs).
 */

/**
 * Sentinela reservado para "desvincular a praça" em `UpdateProductRequest.stationId` — o
 * back-end (`UpdateProductCommand`) usa `null` para "não alterar" (mesma convenção de
 * `UpdateStationRequest`), então limpar um campo opcional via PATCH precisa de um valor
 * explícito e reservado. Ver docstring de `UpdateProductCommand` no back-end.
 */
export const NO_STATION_SENTINEL = '00000000-0000-0000-0000-000000000000';

export const createProductRequestSchema = z.object({
  categoryId: z.string().uuid('Selecione uma categoria'),
  name: z.string().trim().min(1, 'Informe um nome').max(150),
  stationId: z.string().uuid().optional(),
  description: z.string().trim().max(1000).optional(),
  ingredientsText: z.string().trim().max(1000).optional(),
  allergens: z.array(z.string()).optional(),
  allowsFractions: z.boolean().default(false),
  maxFractions: z.number().int().min(1).default(1),
  position: z.number().int().nonnegative().default(0),
  isActive: z.boolean().default(true),
});

export const updateProductRequestSchema = z
  .object({
    name: z.string().trim().min(1, 'Informe um nome').max(150).optional(),
    categoryId: z.string().uuid().optional(),
    stationId: z.string().uuid().optional(),
    description: z.string().trim().max(1000).optional(),
    ingredientsText: z.string().trim().max(1000).optional(),
    allergens: z.array(z.string()).optional(),
    allowsFractions: z.boolean().optional(),
    maxFractions: z.number().int().min(1).optional(),
    position: z.number().int().nonnegative().optional(),
  })
  .refine(
    (value) =>
      value.name !== undefined ||
      value.categoryId !== undefined ||
      value.stationId !== undefined ||
      value.description !== undefined ||
      value.ingredientsText !== undefined ||
      value.allergens !== undefined ||
      value.allowsFractions !== undefined ||
      value.maxFractions !== undefined ||
      value.position !== undefined,
    { message: 'Informe ao menos uma alteração' },
  );

export const reorderProductsRequestSchema = z.object({
  categoryId: z.string().uuid(),
  order: z.array(z.string().uuid()).min(1, 'Informe a nova ordem dos produtos'),
});

export const productSchema = z.object({
  id: z.string().uuid(),
  categoryId: z.string().uuid(),
  categoryName: z.string().min(1),
  stationId: z.string().uuid().nullable(),
  stationName: z.string().nullable(),
  name: z.string().min(1),
  description: z.string().nullable(),
  ingredientsText: z.string().nullable(),
  allergens: z.array(z.string()),
  /** Nulo quando o produto não tem foto — o cardápio exibe um marcador visual neutro (US-010 §4). */
  imageUrl: z.string().nullable(),
  position: z.number().int(),
  isActive: z.boolean(),
  isAvailable: z.boolean(),
  allowsFractions: z.boolean(),
  maxFractions: z.number().int(),
});

export const productListResponseSchema = z.object({
  items: z.array(productSchema),
});

export const prepareProductImageUploadRequestSchema = z.object({
  contentType: z.enum(['image/png', 'image/jpeg', 'image/webp', 'image/heic', 'image/heif']),
  bytes: z.number().int().positive().max(10_000_000, 'O arquivo deve ter no máximo 10 MB'),
  sha256: z.string().regex(/^[0-9a-fA-F]{64}$/, 'Hash SHA-256 inválido'),
});

export const prepareProductImageUploadResponseSchema = z.object({
  uploadUrl: z.string().url(),
  publicUrl: z.string().url(),
  expiresAt: z.string(),
});

export const confirmProductImageRequestSchema = z.object({
  url: z.string().url(),
  contentType: z.enum(['image/png', 'image/jpeg', 'image/webp', 'image/heic', 'image/heif']),
  bytes: z.number().int().positive().max(10_000_000, 'O arquivo deve ter no máximo 10 MB'),
  sha256: z.string().regex(/^[0-9a-fA-F]{64}$/),
  width: z.number().int().min(800, 'A imagem deve ter pelo menos 800 px de largura'),
  height: z.number().int().min(600, 'A imagem deve ter pelo menos 600 px de altura'),
});

export const productImageResponseSchema = z.object({
  mediaAssetId: z.string().uuid(),
  url: z.string().url(),
});

/** Cardápio público (`GET /v1/public/menu`) — usado pelo cardápio da mesa/PWA/delivery, fora do escopo de `apps/web-admin`. */
export const publicMenuProductSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  description: z.string().nullable(),
  ingredientsText: z.string().nullable(),
  allergens: z.array(z.string()),
  imageUrl: z.string().nullable(),
  position: z.number().int(),
  fromPrice: z
    .string()
    .regex(/^\d+\.\d{2}$/)
    .nullable(),
});

export const publicMenuCategorySchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  description: z.string().nullable(),
  position: z.number().int(),
  products: z.array(publicMenuProductSchema),
});

export const publicMenuResponseSchema = z.object({
  tenantId: z.string().uuid(),
  tenantName: z.string().min(1),
  categories: z.array(publicMenuCategorySchema),
});

export type CreateProductRequest = z.infer<typeof createProductRequestSchema>;
export type UpdateProductRequest = z.infer<typeof updateProductRequestSchema>;
export type ReorderProductsRequest = z.infer<typeof reorderProductsRequestSchema>;
export type ProductDto = z.infer<typeof productSchema>;
export type ProductListResponse = z.infer<typeof productListResponseSchema>;
export type PrepareProductImageUploadRequest = z.infer<
  typeof prepareProductImageUploadRequestSchema
>;
export type PrepareProductImageUploadResponse = z.infer<
  typeof prepareProductImageUploadResponseSchema
>;
export type ConfirmProductImageRequest = z.infer<typeof confirmProductImageRequestSchema>;
export type ProductImageResponse = z.infer<typeof productImageResponseSchema>;
export type PublicMenuProductDto = z.infer<typeof publicMenuProductSchema>;
export type PublicMenuCategoryDto = z.infer<typeof publicMenuCategorySchema>;
export type PublicMenuResponse = z.infer<typeof publicMenuResponseSchema>;
