import { z } from 'zod';

/**
 * US-046 (Histórico do turno no KDS) — porta de
 * `Nexora.Contracts.Operation.KdsHistoryContracts` (backend/src/Nexora.Contracts/Operation/KdsHistoryContracts.cs).
 * Arquivo dedicado (não em `kds.ts`) para não colidir com quem mantém aquele arquivo (US-031, fila
 * ativa) nesta mesma onda em paralelo.
 */
export const kdsHistoryOperatorSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
});

export const kdsHistoryItemSchema = z.object({
  orderItemId: z.string().uuid(),
  orderId: z.string().uuid(),
  orderCode: z.string(),
  productName: z.string(),
  table: z.string().nullable(),
  /** T1 (US-032) — nulo no caso residual de item servido sem ter passado por Fire (fluxo manual/legado). */
  firedAt: z.string().nullable(),
  /** T4 (US-032) — nulo pelo mesmo motivo de `firedAt`. */
  readyAt: z.string().nullable(),
  servedAt: z.string(),
  /** Segundos entre `firedAt` e `readyAt` — 0 quando um dos dois carimbos falta. */
  prepSeconds: z.number().int(),
  operator: kdsHistoryOperatorSchema.nullable(),
});

export const kdsHistorySummarySchema = z.object({
  count: z.number().int(),
  avgPrepSeconds: z.number().int(),
});

/** Porta de `GET /v1/kds/history?shift=current&stationId=...&search=...` (US-046 §7). */
export const getKdsHistoryResponseSchema = z.object({
  items: z.array(kdsHistoryItemSchema),
  summary: kdsHistorySummarySchema,
});

export type KdsHistoryOperator = z.infer<typeof kdsHistoryOperatorSchema>;
export type KdsHistoryItem = z.infer<typeof kdsHistoryItemSchema>;
export type KdsHistorySummary = z.infer<typeof kdsHistorySummarySchema>;
export type GetKdsHistoryResponse = z.infer<typeof getKdsHistoryResponseSchema>;
