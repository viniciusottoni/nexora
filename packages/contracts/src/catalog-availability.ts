import { z } from 'zod';

/**
 * US-015 (Marcar produto indisponível com propagação imediata) — porta de
 * `Nexora.Contracts.Catalog.{MarkProductUnavailableRequest,ProductAvailabilityResponse}`
 * (backend/src/Nexora.Contracts/Catalog/MarkProductUnavailableRequest.cs,
 * ProductAvailabilityResponse.cs).
 */

export const productAvailabilityReasonSchema = z
  .string()
  .trim()
  .min(1, 'Informe o motivo da indisponibilidade')
  .max(200);

/** Corpo de `POST /v1/catalog/products/:id/availability` (nuvem) — cobre as duas direções. */
export const setProductAvailabilityRequestSchema = z
  .object({
    isAvailable: z.boolean(),
    reason: productAvailabilityReasonSchema.optional(),
    autoRestoreNextDay: z.boolean().default(true),
  })
  .refine((value) => value.isAvailable || (value.reason?.length ?? 0) > 0, {
    message: 'Informe o motivo da indisponibilidade',
    path: ['reason'],
  });

/** Corpo de `POST /v1/kds/products/:id/unavailable` (edge/KDS) — "cabe em um toque" (US-015 §10). */
export const markProductUnavailableRequestSchema = z.object({
  reason: productAvailabilityReasonSchema,
  autoRestoreNextDay: z.boolean().default(true),
});

export const productAvailabilitySchema = z.object({
  productId: z.string().uuid(),
  productName: z.string().min(1),
  isAvailable: z.boolean(),
  unavailableReason: z.string().nullable(),
  unavailableSince: z.string().nullable(),
});

export const unavailableProductsResponseSchema = z.object({
  items: z.array(productAvailabilitySchema),
});

/**
 * Mensagem do hub SignalR `CatalogAvailabilityHub` (US-015 §7) — o servidor invoca o método
 * `"productAvailabilityChanged"` em todos os clientes do grupo do tenant com este payload.
 */
export const productAvailabilityChangedEventSchema = z.object({
  type: z.enum(['product.unavailable', 'product.available']),
  data: z.object({
    productId: z.string().uuid(),
    reason: z.string().optional(),
    unavailableSince: z.string().optional(),
  }),
});

export type SetProductAvailabilityRequest = z.infer<typeof setProductAvailabilityRequestSchema>;
export type MarkProductUnavailableRequest = z.infer<typeof markProductUnavailableRequestSchema>;
export type ProductAvailabilityDto = z.infer<typeof productAvailabilitySchema>;
export type UnavailableProductsResponse = z.infer<typeof unavailableProductsResponseSchema>;
export type ProductAvailabilityChangedEvent = z.infer<typeof productAvailabilityChangedEventSchema>;
