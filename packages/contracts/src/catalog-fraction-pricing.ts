import { z } from 'zod';
import { catalogChannelSchema } from './catalog-variants.js';

/**
 * US-013 (Pizza meio a meio com frações) — porta de
 * `Nexora.Contracts.Catalog.{PreviewFractionPricingRequest,PreviewFractionPricingResponse}`
 * (backend/src/Nexora.Contracts/Catalog/PreviewFractionPricing{Request,Response}.cs) e de
 * `Nexora.Application.Catalog.FractionPricing.FractionPriceRule`.
 *
 * DESVIO DELIBERADO da convenção de `moneyStringSchema` usada em `catalog-variants.ts`/
 * `catalog-prices.ts` (dinheiro como string decimal, ADR-017): aquelas duas assumem um
 * `JsonConverter<decimal>` dedicado no back-end que, na prática, esta solution só aplica hoje a
 * `Nexora.Contracts.Catalog.Modifier*` — os próprios contratos irmãos de preço (`VariantChannelPriceRow`,
 * `ChannelPriceEntry`, `BulkAdjustPricesResponse`) já trafegam `decimal` como `number` JSON puro,
 * sem conversor nenhum. Como o backend E o frontend deste endpoint foram escritos juntos nesta
 * mesma tarefa, optou-se por manter os dois lados coerentes ENTRE SI (número, não string) em vez
 * de seguir uma convenção que os próprios irmãos já não cumprem — reproduzir `moneyStringSchema`
 * aqui quebraria a integração real com `PreviewFractionPricingQueryHandler`.
 */
export const fractionSelectionSchema = z.object({
  variantId: z.string().uuid(),
  weight: z
    .number()
    .gt(0, 'O peso da fração deve ser maior que zero')
    .lte(1, 'O peso da fração não pode ultrapassar 1,0'),
});

export const previewFractionPricingRequestSchema = z.object({
  fractions: z
    .array(fractionSelectionSchema)
    .min(2, 'Um item meio a meio precisa de ao menos duas frações'),
  channel: catalogChannelSchema.optional(),
});

/** As três regras de precificação de RN-009 — a escolha vigente vive em `tenant_config.operation.fractionPriceRule`. */
export const fractionPriceRuleSchema = z.enum(['HIGHEST', 'AVERAGE', 'PROPORTIONAL']);

export const fractionPricingLineSchema = z.object({
  variantId: z.string().uuid(),
  weight: z.number(),
  unitPrice: z.number(),
});

/** Resposta de `POST /v1/catalog/fraction-pricing/preview` — preço final, regra aplicada e descrição composta (sem persistir nada). */
export const previewFractionPricingResponseSchema = z.object({
  unitPrice: z.number(),
  priceRule: fractionPriceRuleSchema,
  description: z.string(),
  fractions: z.array(fractionPricingLineSchema),
});

export type FractionSelection = z.infer<typeof fractionSelectionSchema>;
export type PreviewFractionPricingRequest = z.infer<typeof previewFractionPricingRequestSchema>;
export type FractionPriceRule = z.infer<typeof fractionPriceRuleSchema>;
export type FractionPricingLine = z.infer<typeof fractionPricingLineSchema>;
export type PreviewFractionPricingResponse = z.infer<typeof previewFractionPricingResponseSchema>;
