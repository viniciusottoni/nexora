import { z } from 'zod';

/**
 * US-014 (Preço por canal de venda) — porta de `Nexora.Contracts.Catalog.VariantPriceTableResponse`/
 * `SetVariantChannelPriceRequest`/`BulkAdjustPricesRequest`
 * (backend/src/Nexora.Contracts/Catalog/{VariantPriceTableResponse,SetVariantChannelPriceRequest,BulkAdjustPricesRequest}.cs).
 *
 * NOTA DE INTEGRAÇÃO: no momento em que este arquivo foi escrito, este worktree ainda não tinha
 * `packages/contracts/src/catalog-variants.ts` (US-011 não tinha camada de Application/Contracts
 * aqui, só `Nexora.Domain`/persistência) — por isso o enum de canal é redefinido localmente como
 * `pricingChannelSchema` em vez de importado de `catalog-variants.js`. Depois que a US-011 "de
 * verdade" for mesclada (ela provavelmente já exporta um `catalogChannelSchema` equivalente), quem
 * integrar pode trocar este import por aquele — os valores são idênticos
 * (`DineIn`/`Delivery`/`Takeout`/`Marketplace`, o texto de `Nexora.Domain.Catalog.Channel`).
 *
 * Dinheiro trafega como **string decimal** (ex.: `"45.90"`), nunca `number` (ADR-017).
 */
const moneyStringSchema = z
  .string()
  .regex(/^\d+\.\d{2}$/, 'Valor monetário inválido — use o formato "0.00"');

export const pricingChannelSchema = z.enum(['DineIn', 'Delivery', 'Takeout', 'Marketplace']);
export type PricingChannel = z.infer<typeof pricingChannelSchema>;

/** Uma linha da tabela de preço por canal — `amount` nulo só no caso defensivo em que nem o canal nem a base (`DineIn`) têm preço vigente. */
export const variantChannelPriceRowSchema = z.object({
  channel: pricingChannelSchema,
  amount: moneyStringSchema.nullable(),
  isInherited: z.boolean(),
  validFrom: z.string().nullable(),
});

/** Resposta de `GET /v1/catalog/variants/:id/prices` — sempre as quatro linhas de canal, com ou sem preço próprio. */
export const variantPriceTableResponseSchema = z.object({
  variantId: z.string().uuid(),
  productId: z.string().uuid(),
  channels: z.array(variantChannelPriceRowSchema).length(4),
});

export const channelPriceEntrySchema = z.object({
  channel: pricingChannelSchema,
  amount: moneyStringSchema,
});

/** Corpo de `PUT /v1/catalog/variants/:id/prices` — define um ou mais canais na mesma chamada (diferente do `POST` de canal único da US-011). */
export const setVariantChannelPriceRequestSchema = z.object({
  prices: z.array(channelPriceEntrySchema).min(1, 'Informe ao menos um preço por canal'),
});

/** Corpo de `POST /v1/catalog/prices/bulk-adjust` — reajuste percentual em um canal para todas as variações ativas de uma categoria. */
export const bulkAdjustPricesRequestSchema = z.object({
  categoryId: z.string().uuid(),
  channel: pricingChannelSchema,
  percent: z.number().min(-100, 'O reajuste não pode reduzir o preço abaixo de zero'),
});

export const bulkAdjustPricesResponseSchema = z.object({
  updated: z.number().int().nonnegative(),
  effectiveFrom: z.string(),
});

export type VariantChannelPriceRow = z.infer<typeof variantChannelPriceRowSchema>;
export type VariantPriceTableResponse = z.infer<typeof variantPriceTableResponseSchema>;
export type ChannelPriceEntry = z.infer<typeof channelPriceEntrySchema>;
export type SetVariantChannelPriceRequest = z.infer<typeof setVariantChannelPriceRequestSchema>;
export type BulkAdjustPricesRequest = z.infer<typeof bulkAdjustPricesRequestSchema>;
export type BulkAdjustPricesResponse = z.infer<typeof bulkAdjustPricesResponseSchema>;
