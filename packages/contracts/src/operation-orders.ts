import { z } from 'zod';

/**
 * US-024 (Consumo da mesa em tempo real) e US-028 (Repetir item com um toque) — porta de
 * `Nexora.Contracts.Operation.OrderContracts` (backend/src/Nexora.Contracts/Operation/OrderContracts.cs).
 * Dinheiro sempre como string (ADR-017) — mesma convenção de `operation-table-sessions.ts`.
 */
const moneyStringSchema = z.string().regex(/^-?\d+(\.\d+)?$/, 'Valor monetário inválido');

/** Mesmo vocabulário técnico de `packages/ui/src/components/status-pill.tsx` (`StatusPillStatus`). */
export const orderItemWireStatusSchema = z.enum([
  'QUEUED',
  'FIRED',
  'IN_OVEN',
  'OUT_OF_OVEN',
  'READY',
  'SERVED',
  'CANCELLED',
]);

export const addOrderItemModifierRequestSchema = z.object({
  modifierId: z.string().uuid(),
  quantity: z.number().int().min(1),
});

export const addOrderItemFractionRequestSchema = z.object({
  variantId: z.string().uuid(),
  weight: z.number().gt(0).lte(1),
});

/** Porta de `POST /v1/sessions/{sessionId}/items`. */
export const addOrderItemRequestSchema = z.object({
  variantId: z.string().uuid(),
  quantity: z.number().int().min(1).max(99),
  notes: z.string().nullable().optional(),
  modifiers: z.array(addOrderItemModifierRequestSchema).nullable().optional(),
  fractions: z.array(addOrderItemFractionRequestSchema).nullable().optional(),
});

export const orderItemModifierResponseSchema = z.object({
  modifierId: z.string().uuid(),
  name: z.string(),
  quantity: z.number().int(),
  priceDelta: moneyStringSchema,
});

export const orderItemFractionResponseSchema = z.object({
  variantId: z.string().uuid(),
  weight: z.number(),
  unitPrice: moneyStringSchema,
});

/** Item lançado — retorno de `POST /v1/sessions/{sessionId}/items` e `POST .../repeat` (US-028 §7). */
export const orderItemResponseSchema = z.object({
  id: z.string().uuid(),
  orderId: z.string().uuid(),
  variantId: z.string().uuid(),
  name: z.string(),
  quantity: z.number().int(),
  unitPrice: moneyStringSchema,
  totalPrice: moneyStringSchema,
  status: orderItemWireStatusSchema,
  notes: z.string().nullable(),
  stationId: z.string().uuid().nullable(),
  repeatedFromItemId: z.string().uuid().nullable(),
  modifiers: z.array(orderItemModifierResponseSchema),
  fractions: z.array(orderItemFractionResponseSchema),
});

/** Envelope exato do contrato da US-028 §7: `{ "item": {...} }`. */
export const repeatOrderItemResponseSchema = z.object({
  item: orderItemResponseSchema,
});

/** Item da lista de consumo (US-024 §7) — status já traduzido para a linguagem do cliente. */
export const sessionConsumptionItemSchema = z.object({
  orderItemId: z.string().uuid(),
  orderId: z.string().uuid(),
  name: z.string(),
  quantity: z.number().int(),
  unitPrice: moneyStringSchema,
  total: moneyStringSchema,
  status: orderItemWireStatusSchema,
  statusLabel: z.string(),
  etaMinutes: z.number().int().nullable(),
  cancelled: z.boolean(),
  variantId: z.string().uuid(),
  productAvailable: z.boolean(),
});

/** Porta de `GET /v1/public/sessions/current` (US-024 §7). */
export const sessionConsumptionResponseSchema = z.object({
  items: z.array(sessionConsumptionItemSchema),
  subtotal: moneyStringSchema,
  serviceFee: moneyStringSchema,
  serviceFeeOptional: z.boolean(),
  total: moneyStringSchema,
  openedAt: z.string(),
  minutesOpen: z.number().int(),
});

/** Mensagem recebida via SignalR (`tableConsumptionChanged`) ou via polling de fallback (ADR-011). */
export const tableConsumptionEventSchema = z.object({
  type: z.string(),
  data: z.object({
    orderItemId: z.string().uuid(),
    productName: z.string().optional(),
    repeatedFrom: z.string().uuid().nullable().optional(),
    status: z.string().optional(),
  }),
});

export type OrderItemWireStatus = z.infer<typeof orderItemWireStatusSchema>;
export type AddOrderItemRequest = z.infer<typeof addOrderItemRequestSchema>;
export type OrderItemModifierDto = z.infer<typeof orderItemModifierResponseSchema>;
export type OrderItemFractionDto = z.infer<typeof orderItemFractionResponseSchema>;
export type OrderItemResponseDto = z.infer<typeof orderItemResponseSchema>;
export type RepeatOrderItemResponse = z.infer<typeof repeatOrderItemResponseSchema>;
export type SessionConsumptionItemDto = z.infer<typeof sessionConsumptionItemSchema>;
export type SessionConsumptionResponse = z.infer<typeof sessionConsumptionResponseSchema>;
export type TableConsumptionEvent = z.infer<typeof tableConsumptionEventSchema>;
