import { z } from 'zod';

/**
 * US-011 (Variações de produto com preço próprio) — porta de `Nexora.Contracts.Catalog.Variant*`
 * (backend/src/Nexora.Contracts/Catalog/VariantRequests.cs, VariantResponses.cs).
 *
 * Dinheiro trafega como **string decimal** (ex.: `"45.90"`), nunca `number` — ADR-017:
 * `double`/`float` são proibidos para dinheiro, e o back-end serializa `decimal` como string via
 * `JsonConverter<decimal>` dedicado, justamente para não perder precisão no JSON. O componente de
 * UI (`variant-price-editor.tsx`) converte entre essa string e a máscara de moeda em centavos
 * usando aritmética inteira, sem `parseFloat`/`Number` na etapa de edição.
 */
const moneyStringSchema = z
  .string()
  .regex(/^\d+\.\d{2}$/, 'Valor monetário inválido — use o formato "0.00"');

/**
 * Canal de venda (`Nexora.Domain.Catalog.Channel`) — só o suficiente para o preço "base" desta
 * história (US-011 §"Esta US só precisa entregar UM preço 'base'"). A tabela de preço por canal
 * completa (todos os canais editáveis simultaneamente, ajuste em massa) é escopo da US-014.
 */
export const catalogChannelSchema = z.enum(['DineIn', 'Delivery', 'Takeout', 'Marketplace']);
export type CatalogChannel = z.infer<typeof catalogChannelSchema>;

export const createVariantRequestSchema = z.object({
  name: z.string().trim().min(1, 'Informe um nome').max(150),
  sizeCode: z.string().trim().max(16).optional(),
  sku: z.string().trim().max(40).optional(),
  prepMinutes: z.number().int().nonnegative().optional(),
  isDefault: z.boolean().default(false),
  basePrice: moneyStringSchema,
  channel: catalogChannelSchema.optional(),
});

export const updateVariantRequestSchema = z.object({
  name: z.string().trim().min(1, 'Informe um nome').max(150),
  sizeCode: z.string().trim().max(16).optional(),
  sku: z.string().trim().max(40).optional(),
});

export const variantSchema = z.object({
  id: z.string().uuid(),
  productId: z.string().uuid(),
  name: z.string().min(1),
  sku: z.string().nullable(),
  sizeCode: z.string().nullable(),
  prepMinutes: z.number().int(),
  isDefault: z.boolean(),
  isActive: z.boolean(),
  /** Nulo quando a variante ainda não tem preço definido no canal consultado. */
  currentPrice: moneyStringSchema.nullable(),
  currentPriceChannel: catalogChannelSchema.nullable(),
});

export const variantListResponseSchema = z.object({
  items: z.array(variantSchema),
});

export type CreateVariantRequest = z.infer<typeof createVariantRequestSchema>;
export type UpdateVariantRequest = z.infer<typeof updateVariantRequestSchema>;
export type VariantDto = z.infer<typeof variantSchema>;
export type VariantListResponse = z.infer<typeof variantListResponseSchema>;
