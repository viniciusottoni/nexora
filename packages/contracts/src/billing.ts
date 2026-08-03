import { z } from 'zod';

/**
 * US-027 (Dividir a conta) — porta de `Nexora.Contracts.Operation.BillContracts`
 * (backend/src/Nexora.Contracts/Operation/BillContracts.cs). Dinheiro sempre como string
 * (ADR-017) — mesma convenção de `operation-orders.ts`/`operation-table-sessions.ts`.
 */
const moneyStringSchema = z.string().regex(/^-?\d+(\.\d+)?$/, 'Valor monetário inválido');

/**
 * Modos de divisão desta história — superconjunto de `billSplitModeSchema` (US-026, sem
 * `BY_AMOUNT`: aquele é só a PREFERÊNCIA registrada na solicitação da conta, e "pagar um valor
 * arbitrário" nunca foi uma preferência de toda a conta, só do cálculo desta história).
 */
export const billSplitModeWithAmountSchema = z.enum(['BY_PERSON', 'BY_ITEM', 'SINGLE', 'BY_AMOUNT']);

export const billItemSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  total: moneyStringSchema,
  pending: z.boolean(),
  assignedPerson: z.number().int().positive().nullable(),
});

/** Item ainda em produção no momento da divisão (US-027 §4, cenário "Item pendente durante a divisão"). */
export const billPendingItemSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  status: z.string(),
});

/** Parte de uma pessoa na divisão — `serviceFeeAmount` sempre identificado separadamente (US-027 §4). */
export const billSplitPartSchema = z.object({
  person: z.number().int().positive(),
  amount: moneyStringSchema,
  serviceFeeAmount: moneyStringSchema,
  serviceFeeWaived: z.boolean(),
});

/**
 * US-035 (Bloquear fechamento com item pendente) — modo configurado pelo tenant para
 * `pendingItems`/`hasPendingItems` acima: `BLOCK` desabilita o fechamento até autorizar ou os
 * itens serem resolvidos, `WARN` só avisa (comportamento anterior a esta história), `IGNORE` nem
 * mostra o aviso.
 */
export const pendingItemsModeSchema = z.enum(['BLOCK', 'WARN', 'IGNORE']);

/**
 * Porta de `GET /v1/sessions/{id}/bill` (staff) e `GET /v1/public/sessions/current/bill`
 * (cliente, pré-visualização) — mesmo formato para as duas audiências (US-027 §10).
 */
export const billResponseSchema = z.object({
  items: z.array(billItemSchema),
  subtotal: moneyStringSchema,
  serviceFee: moneyStringSchema,
  total: moneyStringSchema,
  splitMode: billSplitModeWithAmountSchema,
  split: z.array(billSplitPartSchema),
  pendingItems: z.array(billPendingItemSchema),
  hasPendingItems: z.boolean(),
  amountPaid: moneyStringSchema.nullable(),
  remainingAmount: moneyStringSchema.nullable(),
  unassignedItemIds: z.array(z.string().uuid()),
  // Opcional (sem `.default()`) para não exigir o campo em fixtures/mocks anteriores a esta
  // história — ausente é tratado como `'WARN'` pelo consumidor (mesmo default do backend).
  pendingItemsMode: pendingItemsModeSchema.optional(),
});

export const billItemAssignmentRequestSchema = z.object({
  person: z.number().int().positive(),
  itemIds: z.array(z.string().uuid()),
});

/**
 * Porta de `POST /v1/sessions/{id}/bill/assign-items` (US-027 §7). [DECISÃO] Não persistida no
 * backend (US-027 §6: "a divisão é cálculo, não fato de negócio") — o POS reenvia a atribuição
 * completa a cada chamada.
 */
export const assignBillItemsRequestSchema = z.object({
  // Lista vazia é válida (ex.: nenhum item foi tocado ainda) — a recusa de negócio "item não
  // atribuído" (RN-017, código BILL_ITEM_NOT_ASSIGNED) é do backend, não deste schema.
  assignments: z.array(billItemAssignmentRequestSchema),
  serviceFeeWaivedPersons: z.array(z.number().int().positive()).nullable().optional(),
});

/**
 * Porta de `POST /v1/sessions/{id}/bill/waive-service-fee` (US-027 §4, cenário "Retirada da
 * taxa por uma das partes") — `alreadyWaivedPersons` é o conjunto acumulado ANTES desta chamada
 * (o cliente mantém esse estado, já que a divisão não é persistida).
 */
export const waiveServiceFeeRequestSchema = z.object({
  people: z.number().int().positive(),
  person: z.number().int().positive(),
  alreadyWaivedPersons: z.array(z.number().int().positive()).nullable().optional(),
  reason: z.string().nullable().optional(),
});

const paymentMethodSchema = z.enum(['CASH', 'CREDIT', 'DEBIT', 'PIX', 'ONLINE', 'VOUCHER', 'OTHER']);

/**
 * Porta de `POST /v1/sessions/{id}/bill/partial-payment` (US-027 §4, cenário "Divisão por valor").
 * `reason` (US-035 §10) só é relevante ao reenviar esta mesma chamada com `X-Authorization-Token`
 * depois de um 422 `PENDING_ITEMS` — motivo registrado no `AuditLog` junto do autorizador.
 */
export const registerPartialPaymentRequestSchema = z.object({
  amount: z.number().positive(),
  method: paymentMethodSchema,
  reason: z.string().nullable().optional(),
});

/**
 * `remainingAmount` é o saldo que continua em aberto — a sessão permanece em `BILLREQUESTED`.
 * `pendingItems` (US-035) só vem preenchido no modo `WARN` com item pendente.
 */
export const partialPaymentResponseSchema = z.object({
  paymentId: z.string().uuid(),
  amountPaid: moneyStringSchema,
  remainingAmount: moneyStringSchema,
  total: moneyStringSchema,
  sessionStatus: z.string(),
  pendingItems: z.array(billPendingItemSchema).nullable().optional(),
});

export type BillSplitModeWithAmount = z.infer<typeof billSplitModeWithAmountSchema>;
export type PendingItemsMode = z.infer<typeof pendingItemsModeSchema>;
export type BillItemDto = z.infer<typeof billItemSchema>;
export type BillPendingItemDto = z.infer<typeof billPendingItemSchema>;
export type BillSplitPartDto = z.infer<typeof billSplitPartSchema>;
export type BillResponse = z.infer<typeof billResponseSchema>;
export type BillItemAssignmentRequest = z.infer<typeof billItemAssignmentRequestSchema>;
export type AssignBillItemsRequest = z.infer<typeof assignBillItemsRequestSchema>;
export type WaiveServiceFeeRequest = z.infer<typeof waiveServiceFeeRequestSchema>;
export type RegisterPartialPaymentRequest = z.infer<typeof registerPartialPaymentRequestSchema>;
export type PartialPaymentResponse = z.infer<typeof partialPaymentResponseSchema>;
