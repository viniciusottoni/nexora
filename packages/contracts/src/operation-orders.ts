import { z } from 'zod';
import { catalogChannelSchema } from './catalog-variants.js';

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

/**
 * US-030 (Criar pedido com itens, modificadores e frações) — porta de
 * `Nexora.Contracts.Operation.{CreateOrderItemRequest,CreateOrderRequest,CreatePublicOrderRequest,
 * OrderResponse,CreateOrderResponse}` (backend/src/Nexora.Contracts/Operation/OrderContracts.cs).
 * Reaproveita os schemas de modificador/fração já existentes acima — mesma FORMA de
 * `AddOrderItemModifierRequest`/`AddOrderItemFractionRequest` (o C# tem registros duplicados
 * porque nascem de contratos distintos e podem evoluir separadamente; aqui um único zod schema
 * já serve aos dois lados, sem esse motivo para duplicar).
 */
export const createOrderItemRequestSchema = z.object({
  variantId: z.string().uuid(),
  quantity: z.number().int().min(1).max(99),
  notes: z.string().trim().max(500).nullable().optional(),
  modifiers: z.array(addOrderItemModifierRequestSchema).nullable().optional(),
  fractions: z.array(addOrderItemFractionRequestSchema).nullable().optional(),
});

/**
 * Porta de `POST /v1/orders` (US-030 §7) — garçom/POS autenticado: `channel`/`sessionId` vêm do
 * corpo (o garçom escolhe a mesa e o canal explicitamente). `channel` usa o MESMO vocabulário de
 * fio do resto do catálogo (`"DineIn"`/`"Delivery"`/`"Takeout"`/`"Marketplace"`, `catalogChannelSchema`
 * de `catalog-variants.ts`) — NÃO o `"DINE_IN"` ilustrado na spec narrativa da história, que diverge
 * da convenção real já em produção (ver docstring de `CreateOrderRequest` no backend).
 */
export const createOrderRequestSchema = z.object({
  channel: catalogChannelSchema,
  sessionId: z.string().uuid().nullable().optional(),
  items: z.array(createOrderItemRequestSchema).min(1, 'Inclua ao menos um item no pedido'),
});

/**
 * Porta de `POST /v1/public/orders` (US-030 §7, caminho do cliente na mesa via QR) — SEM
 * `channel`/`sessionId`: os dois vêm das claims do token de sessão de mesa (RN-015), nunca do
 * corpo que o cliente controla.
 */
export const createPublicOrderRequestSchema = z.object({
  items: z.array(createOrderItemRequestSchema).min(1, 'Inclua ao menos um item no pedido'),
});

/**
 * Vocabulário de fio de `Nexora.Application.Orders.Support.OrderStatusLabels.ToWireStatus`
 * (upper snake_case) — DISTINTO do vocabulário de item (`orderItemWireStatusSchema` acima):
 * pedido e item têm máquinas de estado próprias (doc. 04).
 */
export const orderWireStatusSchema = z.enum([
  'DRAFT',
  'PLACED',
  'IN_PRODUCTION',
  'READY',
  'DISPATCHED',
  'DELIVERED',
  'CLOSED',
  'CANCELLED',
]);

/**
 * Pedido dentro do envelope de `POST /v1/orders`/`POST /v1/public/orders`/`GET /v1/orders/{id}`
 * (US-030 §7) — só um campo de código (`shortCode`, ex.: "A47"), é o que a cozinha chama em voz
 * alta (US-030 §10).
 */
export const orderResponseSchema = z.object({
  id: z.string().uuid(),
  shortCode: z.string().min(1),
  status: orderWireStatusSchema,
  sessionId: z.string().uuid().nullable(),
  channel: catalogChannelSchema,
  total: moneyStringSchema,
  placedAt: z.string().nullable(),
  items: z.array(orderItemResponseSchema),
});

/** Envelope exato do contrato da US-030 §7: `{ "order": {...}, "promisedAt": ..., "estimatedMinutes": ... }`. */
export const createOrderResponseSchema = z.object({
  order: orderResponseSchema,
  promisedAt: z.string(),
  estimatedMinutes: z.number().int(),
});

/**
 * US-033 (Cancelar item ou pedido com autorização) — porta de
 * `Nexora.Contracts.Operation.{CancelOrderItemRequest,CancelledOrderItemResponse,
 * CancelOrderItemResponse,CancelOrderRequest,CancelledOrderResponse,CancelOrderResponse}`
 * (backend/src/Nexora.Contracts/Operation/OrderContracts.cs). `reason` é o código de uma lista
 * curta e configurável (US-033 §10, ex.: `"CUSTOMER_REQUEST"`) — mora no cliente (Fase 1), nunca
 * hardcoded no domínio (ADR-013).
 */
export const cancelOrderItemRequestSchema = z.object({
  reason: z.string().min(1).max(120),
  notes: z.string().trim().max(500).nullable().optional(),
});

/** Resumo de quem autorizou uma elevação pontual (ADR-023) — mesma forma de `AuthorizationGrant.authorizedBy` de `operational-auth-client.ts`. */
export const authorizedBySummarySchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
});

/**
 * Item cancelado — porta do envelope exato da US-033 §7: `{ "item": { "status": "CANCELLED",
 * "cancelledAt": ..., "wasStarted": ..., "authorizedBy": {...} } }`. `wasStarted` é derivado do
 * estado do item ANTES do cancelamento (RN-008) — sinaliza registro de perda de estoque (US-105,
 * Fase 2, fora de escopo aqui).
 */
export const cancelledOrderItemSchema = z.object({
  id: z.string().uuid(),
  status: orderItemWireStatusSchema,
  cancelledAt: z.string(),
  reason: z.string(),
  notes: z.string().nullable(),
  wasStarted: z.boolean(),
  authorizedBy: authorizedBySummarySchema.nullable(),
});

/** Envelope exato do contrato da US-033 §7: `{ "item": {...} }`. */
export const cancelOrderItemResponseSchema = z.object({
  item: cancelledOrderItemSchema,
});

/** Porta de `POST /v1/orders/{id}/cancel` (US-033 §7) — cancelamento do pedido inteiro. */
export const cancelOrderRequestSchema = z.object({
  reason: z.string().min(1).max(120),
  notes: z.string().trim().max(500).nullable().optional(),
});

/** Pedido cancelado, com o detalhe de cada item cancelado na MESMA operação (US-033 §4, cenário "Cancelamento de pedido inteiro"). */
export const cancelledOrderSchema = z.object({
  id: z.string().uuid(),
  status: orderWireStatusSchema,
  cancelledAt: z.string(),
  reason: z.string(),
  authorizedBy: authorizedBySummarySchema.nullable(),
  items: z.array(cancelledOrderItemSchema),
});

/** Envelope: `{ "order": {...} }` — mesma convenção de `cancelOrderItemResponseSchema`. */
export const cancelOrderResponseSchema = z.object({
  order: cancelledOrderSchema,
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
export type CreateOrderItemRequest = z.infer<typeof createOrderItemRequestSchema>;
export type CreateOrderRequest = z.infer<typeof createOrderRequestSchema>;
export type CreatePublicOrderRequest = z.infer<typeof createPublicOrderRequestSchema>;
export type OrderWireStatus = z.infer<typeof orderWireStatusSchema>;
export type OrderResponseDto = z.infer<typeof orderResponseSchema>;
export type CreateOrderResponse = z.infer<typeof createOrderResponseSchema>;
export type AuthorizedBySummaryDto = z.infer<typeof authorizedBySummarySchema>;
export type CancelOrderItemRequest = z.infer<typeof cancelOrderItemRequestSchema>;
export type CancelledOrderItemDto = z.infer<typeof cancelledOrderItemSchema>;
export type CancelOrderItemResponse = z.infer<typeof cancelOrderItemResponseSchema>;
export type CancelOrderRequest = z.infer<typeof cancelOrderRequestSchema>;
export type CancelledOrderDto = z.infer<typeof cancelledOrderSchema>;
export type CancelOrderResponse = z.infer<typeof cancelOrderResponseSchema>;
