import { z } from 'zod';
import { catalogChannelSchema } from './catalog-variants.js';

/**
 * US-011 (Variações de produto com preço próprio) — porta de `Nexora.Contracts.Catalog.Price*`
 * (backend/src/Nexora.Contracts/Catalog/PriceRequests.cs, PriceResponses.cs). `Price` é
 * historizado e imutável por design (`validFrom`/`validTo`) — `POST .../variants/:id/prices`
 * sempre fecha a linha vigente do canal e cria uma nova; não existe `PATCH` de preço.
 */
const moneyStringSchema = z
  .string()
  .regex(/^\d+\.\d{2}$/, 'Valor monetário inválido — use o formato "0.00"');

export const setVariantPriceRequestSchema = z.object({
  amount: moneyStringSchema,
  channel: catalogChannelSchema.optional(),
});

export const priceSchema = z.object({
  id: z.string().uuid(),
  variantId: z.string().uuid(),
  channel: catalogChannelSchema,
  amount: moneyStringSchema,
  validFrom: z.string(),
  validTo: z.string().nullable(),
});

export type SetVariantPriceRequest = z.infer<typeof setVariantPriceRequestSchema>;
export type PriceDto = z.infer<typeof priceSchema>;
