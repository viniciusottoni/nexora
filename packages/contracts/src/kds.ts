import { z } from 'zod';
import { catalogChannelSchema } from './catalog-variants.js';
import { orderItemWireStatusSchema } from './operation-orders.js';

/**
 * US-031 (Roteamento simultâneo para cozinha e caixa) — porta de
 * `Nexora.Contracts.Operation.KdsContracts` (backend/src/Nexora.Contracts/Operation/KdsContracts.cs)
 * e do payload `kdsEvent` emitido pelo `KdsHub`/`SignalRStationBroadcaster` (ADR-011).
 */
export const kdsQueueItemSchema = z.object({
  orderItemId: z.string().uuid(),
  orderCode: z.string(),
  productName: z.string(),
  quantity: z.number().int(),
  modifiers: z.array(z.string()),
  notes: z.string().nullable(),
  status: orderItemWireStatusSchema,
  placedAt: z.string(),
  elapsedSeconds: z.number().int(),
  table: z.string().nullable(),
  channel: catalogChannelSchema,
});

/** Porta de `GET /v1/kds/queue?stationId=...&since=...` (US-031 §7, fallback de polling do ADR-011). */
export const getKdsQueueResponseSchema = z.object({
  items: z.array(kdsQueueItemSchema),
  lastEventId: z.string(),
});

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
