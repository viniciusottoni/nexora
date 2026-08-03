import { z } from 'zod';
import { billPendingItemSchema } from './billing.js';
import { billSplitModeSchema, tableSessionSchema } from './operation-table-sessions.js';

/**
 * US-025 (Chamar garçom pela mesa) e US-026 (Solicitar a conta) — porta de
 * `Nexora.Contracts.Operation.CallWaiterResponse`/`AcknowledgeWaiterCallResponse`/
 * `RequestBillRequest`/`RequestBillResponse` (backend/src/Nexora.Contracts/Operation/TableSessionContracts.cs).
 */

/** Porta de `POST /v1/public/table/{qrToken}/call-waiter` (US-025 §7). */
export const callWaiterResponseSchema = z.object({
  acknowledged: z.boolean(),
  alreadyPending: z.boolean(),
});

/** Porta de `POST /v1/tables/{id}/acknowledge-call` (US-025 §7). */
export const acknowledgeWaiterCallResponseSchema = z.object({
  resolved: z.boolean(),
  responseSeconds: z.number().int().nonnegative(),
});

/**
 * Porta de `POST /v1/public/table/{qrToken}/request-bill` e `POST /v1/sessions/{id}/request-bill`
 * (US-026 §7). `people` só é exigido quando `splitMode` é `BY_PERSON` (mesma regra do backend).
 */
export const requestBillRequestSchema = z
  .object({
    splitMode: billSplitModeSchema,
    people: z.number().int().positive().optional(),
    // US-035 §10: só relevante ao reenviar esta mesma chamada com X-Authorization-Token depois de
    // um 422 PENDING_ITEMS — motivo registrado no AuditLog junto do autorizador.
    reason: z.string().nullable().optional(),
  })
  .refine((value) => value.splitMode !== 'BY_PERSON' || (value.people !== undefined && value.people > 0), {
    message: 'Informe quantas pessoas vão dividir a conta',
    path: ['people'],
  });

export const requestBillResponseSchema = z.object({
  session: tableSessionSchema,
  alreadyRequested: z.boolean(),
  // US-035: só preenchido no modo WARN com item pendente.
  pendingItems: z.array(billPendingItemSchema).nullable().optional(),
});

export type CallWaiterResponse = z.infer<typeof callWaiterResponseSchema>;
export type AcknowledgeWaiterCallResponse = z.infer<typeof acknowledgeWaiterCallResponseSchema>;
export type RequestBillRequest = z.infer<typeof requestBillRequestSchema>;
export type RequestBillResponse = z.infer<typeof requestBillResponseSchema>;
export type BillSplitMode = z.infer<typeof billSplitModeSchema>;
