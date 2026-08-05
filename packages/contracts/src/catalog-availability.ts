import { z } from 'zod';

/**
 * US-015 (Marcar produto indisponível com propagação imediata) — porta de
 * `Nexora.Contracts.Catalog.{MarkProductUnavailableRequest,ProductAvailabilityResponse}`
 * (backend/src/Nexora.Contracts/Catalog/MarkProductUnavailableRequest.cs,
 * ProductAvailabilityResponse.cs).
 */

/**
 * US-044 §10 — "motivo escolhido por número (1 acabou, 2 equipamento, 3 qualidade), não por
 * texto": porta de `Nexora.Contracts.Catalog.ProductUnavailableReasons`. Os dois processos que
 * gravam `MarkProductUnavailableCommand.Reason` (KDS via edge e painel via nuvem) validam contra a
 * MESMA lista no backend — texto livre (aceito pela US-015 original) não passa mais.
 */
export const PRODUCT_UNAVAILABLE_REASONS = ['OUT_OF_STOCK', 'EQUIPMENT', 'QUALITY'] as const;

export const productUnavailableReasonSchema = z.enum(PRODUCT_UNAVAILABLE_REASONS);

/** Rótulo em PT-BR + tecla numérica de cada motivo — mesma ordem de `ProductUnavailableReasons.All` (índice 0 = tecla "1"). */
export const PRODUCT_UNAVAILABLE_REASON_LABELS: Record<(typeof PRODUCT_UNAVAILABLE_REASONS)[number], string> = {
  OUT_OF_STOCK: 'Acabou',
  EQUIPMENT: 'Equipamento',
  QUALITY: 'Qualidade',
};

/** Mantido para os poucos consumidores que ainda leem `unavailableReason` como texto livre já persistido (ex. histórico). */
export const productAvailabilityReasonSchema = z
  .string()
  .trim()
  .min(1, 'Informe o motivo da indisponibilidade')
  .max(200);

/** Corpo de `POST /v1/catalog/products/:id/availability` (nuvem) — cobre as duas direções. */
export const setProductAvailabilityRequestSchema = z
  .object({
    isAvailable: z.boolean(),
    reason: productUnavailableReasonSchema.optional(),
    autoRestoreNextDay: z.boolean().default(true),
  })
  .refine((value) => value.isAvailable || value.reason !== undefined, {
    message: 'Informe o motivo da indisponibilidade',
    path: ['reason'],
  });

/** Corpo de `POST /v1/kds/products/:id/unavailable` (edge/KDS) — "cabe em um toque" (US-015 §10), motivo por número (US-044 §10). */
export const markProductUnavailableRequestSchema = z.object({
  reason: productUnavailableReasonSchema,
  autoRestoreNextDay: z.boolean().default(true),
  /** US-044 §6 — preenchido só quando a marcação parte de um item específico já na fila do KDS. */
  orderItemId: z.string().uuid().optional(),
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

export type ProductUnavailableReason = z.infer<typeof productUnavailableReasonSchema>;
export type SetProductAvailabilityRequest = z.infer<typeof setProductAvailabilityRequestSchema>;
export type MarkProductUnavailableRequest = z.infer<typeof markProductUnavailableRequestSchema>;
export type ProductAvailabilityDto = z.infer<typeof productAvailabilitySchema>;
export type UnavailableProductsResponse = z.infer<typeof unavailableProductsResponseSchema>;
export type ProductAvailabilityChangedEvent = z.infer<typeof productAvailabilityChangedEventSchema>;
