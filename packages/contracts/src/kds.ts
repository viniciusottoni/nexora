import { z } from 'zod';
import { catalogChannelSchema } from './catalog-variants.js';
import { orderItemWireStatusSchema } from './operation-orders.js';

/**
 * US-031 (Roteamento simultâneo para cozinha e caixa) — porta de
 * `Nexora.Contracts.Operation.KdsContracts` (backend/src/Nexora.Contracts/Operation/KdsContracts.cs)
 * e do payload `kdsEvent` emitido pelo `KdsHub`/`SignalRStationBroadcaster` (ADR-011).
 */
export const kdsQueueItemFractionSchema = z.object({
  productName: z.string(),
  weight: z.string(),
});

/** US-040 §5 — "NORMAL"/"WARNING"/"CRITICAL", já resolvido pelo servidor (ver `KdsContracts.cs`). */
export const kdsThresholdStateSchema = z.enum(['NORMAL', 'WARNING', 'CRITICAL']);

export const kdsQueueItemSchema = z.object({
  orderItemId: z.string().uuid(),
  orderId: z.string().uuid(),
  orderCode: z.string(),
  productId: z.string().uuid(),
  productName: z.string(),
  quantity: z.number().int(),
  modifiers: z.array(z.string()),
  notes: z.string().nullable(),
  status: orderItemWireStatusSchema,
  placedAt: z.string(),
  elapsedSeconds: z.number().int(),
  thresholdState: kdsThresholdStateSchema,
  warnSeconds: z.number().int(),
  criticalSeconds: z.number().int(),
  table: z.string().nullable(),
  channel: catalogChannelSchema,
  fractions: z.array(kdsQueueItemFractionSchema),
});

/** Porta de `GET /v1/kds/queue?stationId=...&since=...` (US-031 §7, fallback de polling do ADR-011). */
export const getKdsQueueResponseSchema = z.object({
  items: z.array(kdsQueueItemSchema),
  lastEventId: z.string(),
});

/** Porta de `OrderItemResponse` (backend/src/Nexora.Contracts/Operation/OrderContracts.cs) — usado pelo retorno de avanço/desfazer do KDS (US-041). */
export const kdsOrderItemResponseSchema = z.object({
  id: z.string().uuid(),
  orderId: z.string().uuid(),
  variantId: z.string().uuid(),
  name: z.string(),
  quantity: z.number().int(),
  unitPrice: z.string(),
  totalPrice: z.string(),
  status: orderItemWireStatusSchema,
  notes: z.string().nullable(),
  stationId: z.string().uuid().nullable(),
  repeatedFromItemId: z.string().uuid().nullable(),
});

/** Porta de `POST /v1/kds/orders/{shortCode}/advance` (US-041). */
export const advanceKdsOrderResponseSchema = z.object({
  advanced: z.array(kdsOrderItemResponseSchema),
});

export type KdsOrderItemResponse = z.infer<typeof kdsOrderItemResponseSchema>;
export type AdvanceKdsOrderResponse = z.infer<typeof advanceKdsOrderResponseSchema>;
export type KdsThresholdState = z.infer<typeof kdsThresholdStateSchema>;

/**
 * Mensagem recebida via SignalR (`kdsEvent`, `KdsHub`) — `order.placed` (pedido novo, `items[]` com
 * `stationId` de cada item) ou `order.item.queued`/`order.item.{status}` (um item isolado). O
 * cliente normaliza os dois formatos para o MESMO tipo de item da fila (ver `kds-realtime.ts`).
 */
export const kdsEventItemSchema = z.object({
  orderItemId: z.string().uuid(),
  productName: z.string(),
  stationId: z.string().uuid().nullable().optional(),
  quantity: z.number().int(),
  modifiers: z.array(z.string()).optional(),
  notes: z.string().nullable().optional(),
  status: z.string().optional(),
});

export const kdsEventSchema = z.object({
  type: z.string(),
  data: z.object({
    orderId: z.string().uuid().optional(),
    code: z.string().optional(),
    shortCode: z.string().optional(),
    table: z.string().nullable().optional(),
    tableId: z.string().uuid().nullable().optional(),
    channel: catalogChannelSchema.optional(),
    items: z.array(kdsEventItemSchema).optional(),
    // order.item.{status} (item isolado, sem items[]) e o replay de KdsHub.Resume mandam os
    // campos do item direto em `data`, não dentro de `items[]`.
    orderItemId: z.string().uuid().optional(),
    productName: z.string().optional(),
    status: z.string().optional(),
  }),
});

export type KdsQueueItem = z.infer<typeof kdsQueueItemSchema>;
export type GetKdsQueueResponse = z.infer<typeof getKdsQueueResponseSchema>;
export type KdsEvent = z.infer<typeof kdsEventSchema>;
export type KdsEventItem = z.infer<typeof kdsEventItemSchema>;
