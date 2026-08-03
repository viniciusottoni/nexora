import { z } from 'zod';

/**
 * US-021/US-022 — porta de `Nexora.Contracts.Operation.TableSessionResponse` e afins
 * (backend/src/Nexora.Contracts/Operation/TableSessionContracts.cs). `total`/dinheiro sempre
 * como string (ADR-017) — mesma convenção de `catalog-prices.ts`.
 */
const moneyStringSchema = z.string().regex(/^-?\d+(\.\d+)?$/, 'Valor monetário inválido');

export const tableSessionItemSchema = z.object({
  name: z.string(),
  quantity: z.number().int().positive(),
  status: z.string(),
});

/** Porta de `POST /v1/tables/{id}/sessions` — abertura pelo garçom (US-022 §7). */
export const openTableSessionRequestSchema = z.object({
  guestCount: z.number().int().min(1, 'Informe quantas pessoas sentaram à mesa').max(200),
});

/**
 * Porta de `PATCH /v1/sessions/{id}` (US-022 §7) — os dois campos são opcionais e
 * independentes; ao menos um precisa estar presente.
 */
export const updateTableSessionRequestSchema = z
  .object({
    guestCount: z.number().int().min(1).max(200).optional(),
    waiterId: z.string().uuid().optional(),
  })
  .refine((value) => value.guestCount !== undefined || value.waiterId !== undefined, {
    message: 'Informe ao menos a nova contagem de pessoas ou o novo garçom responsável',
  });

/** US-026 §7: um de três modos fechados — validado pelo backend, o front só restringe a mesma enumeração. */
export const billSplitModeSchema = z.enum(['BY_PERSON', 'BY_ITEM', 'SINGLE']);

/**
 * Porta de `TableSessionResponse` — devolvida por `POST`/`PATCH`/`GET` de sessão e por `GET /v1/public/table/{qrToken}`.
 * `status` no backend serializa `BillRequested` como `"BILLREQUESTED"` (sem underscore — `enum.ToString().ToUpperInvariant()`,
 * ver `TableSessionMapper`), diferente do valor `BILL_REQUESTED` usado no mapa de mesas (US-023) — os dois contratos
 * nasceram em histórias diferentes e não foram unificados; este schema reflete o valor REAL que o backend emite aqui.
 */
export const tableSessionSchema = z.object({
  id: z.string().uuid(),
  tableId: z.string().uuid(),
  tableLabel: z.string(),
  status: z.enum(['OPEN', 'BILLREQUESTED', 'PAID', 'CLOSED']),
  openedAt: z.string(),
  guestCount: z.number().int(),
  guestCountConfirmed: z.boolean(),
  waiterId: z.string().uuid().nullable(),
  source: z.enum(['WAITER', 'QR']),
  currentItems: z.array(tableSessionItemSchema),
  total: moneyStringSchema,
  /** US-026 §8: preferência de divisão registrada na solicitação da conta — nulo até a primeira solicitação. */
  splitMode: billSplitModeSchema.nullable().optional(),
  splitPeople: z.number().int().positive().nullable().optional(),
});

/** Porta de `GET /v1/public/table/{qrToken}` (US-021 §7). */
export const publicTableInfoSchema = z.object({
  id: z.string().uuid(),
  label: z.string(),
  areaName: z.string(),
});

export const publicTableAccessResponseSchema = z.object({
  table: publicTableInfoSchema,
  session: tableSessionSchema,
  sessionToken: z.string().min(1),
});

export type TableSessionItemDto = z.infer<typeof tableSessionItemSchema>;
export type OpenTableSessionRequest = z.infer<typeof openTableSessionRequestSchema>;
export type UpdateTableSessionRequest = z.infer<typeof updateTableSessionRequestSchema>;
export type TableSessionDto = z.infer<typeof tableSessionSchema>;
export type PublicTableInfoDto = z.infer<typeof publicTableInfoSchema>;
export type PublicTableAccessResponse = z.infer<typeof publicTableAccessResponseSchema>;
