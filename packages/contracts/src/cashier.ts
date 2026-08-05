import { z } from 'zod';

/**
 * US-055 (Abertura e fechamento de caixa) e US-056 (Sangria e suprimento) — porta de
 * `Nexora.Contracts.Cashier.CashierContracts` (backend/src/Nexora.Contracts/Cashier/CashierContracts.cs).
 * Dinheiro sempre como string (ADR-017) — mesma convenção de `billing.ts`.
 */
const moneyStringSchema = z.string().regex(/^-?\d+(\.\d+)?$/, 'Valor monetário inválido');

export const cashSessionStatusSchema = z.enum(['OPEN', 'CLOSING', 'CLOSED']);

export const cashSessionSchema = z.object({
  id: z.string().uuid(),
  operatorId: z.string().uuid(),
  status: cashSessionStatusSchema,
  openingAmount: moneyStringSchema,
  openedAt: z.string(),
  closedAt: z.string().nullable(),
  expectedAmount: moneyStringSchema.nullable(),
  countedAmount: moneyStringSchema.nullable(),
  divergence: moneyStringSchema.nullable(),
  justification: z.string().nullable(),
});

/** Porta de `POST /v1/cash-sessions/open` (US-055 §7). */
export const openCashSessionRequestSchema = z.object({
  openingAmount: z.number().min(0),
});

export const openCashSessionResponseSchema = z.object({
  session: cashSessionSchema,
});

/**
 * Composição do valor esperado (US-055 §4/§10: "a composição deve estar detalhada na tela").
 * `withdrawals` já carrega o sinal negativo — `total` soma direto as quatro parcelas.
 */
export const cashExpectedAmountSchema = z.object({
  opening: moneyStringSchema,
  cashPayments: moneyStringSchema,
  supplies: moneyStringSchema,
  withdrawals: moneyStringSchema,
  total: moneyStringSchema,
});

/** Porta de `GET /v1/cash-sessions/current` (US-055 §7). */
export const getCurrentCashSessionResponseSchema = z.object({
  session: cashSessionSchema,
  expected: cashExpectedAmountSchema,
});

/**
 * Porta de `POST /v1/cash-sessions/{id}/close` (US-055 §7). `justification` é obrigatória quando
 * a divergência ultrapassa o limiar configurado — o servidor recusa com `CASH_JUSTIFICATION_REQUIRED`
 * quando ausente; o cliente reenvia a mesma chamada com o campo preenchido.
 */
export const closeCashSessionRequestSchema = z.object({
  countedAmount: z.number().min(0),
  justification: z.string().nullable().optional(),
});

export const closeCashSessionResponseSchema = z.object({
  expected: moneyStringSchema,
  counted: moneyStringSchema,
  divergence: moneyStringSchema,
  requiresJustification: z.boolean(),
  session: cashSessionSchema,
});

/** Mesa ainda aberta que bloqueia o fechamento (RN-018) — `meta.openSessions` do 422 `OPEN_TABLES` (US-055 §7). */
export const openTableSessionInfoSchema = z.object({
  table: z.string(),
  total: moneyStringSchema,
});

export const cashMovementTypeSchema = z.enum(['WITHDRAWAL', 'SUPPLY']);

export const cashMovementSchema = z.object({
  id: z.string().uuid(),
  type: cashMovementTypeSchema,
  amount: moneyStringSchema,
  reason: z.string(),
  occurredAt: z.string(),
  createdBy: z.string().uuid(),
  authorizedBy: z.string().uuid().nullable(),
});

/**
 * Porta de `POST /v1/cash-sessions/movements` (US-056 §7). `authorizationToken` (ADR-023) só é
 * enviado quando a sangria ultrapassa `operation.maxWithdrawalWithoutAuth` e o gerente já autorizou.
 */
export const registerCashMovementRequestSchema = z.object({
  type: cashMovementTypeSchema,
  amount: z.number().positive(),
  reason: z.string().min(1, 'Informe o motivo do movimento.'),
});

export const registerCashMovementResponseSchema = z.object({
  movement: cashMovementSchema,
  newExpected: moneyStringSchema,
});

/** Porta de `GET /v1/cash-sessions/current/movements` (US-056 §7/§10). */
export const listCashMovementsResponseSchema = z.object({
  movements: z.array(cashMovementSchema),
});

export type CashSessionStatus = z.infer<typeof cashSessionStatusSchema>;
export type CashSessionDto = z.infer<typeof cashSessionSchema>;
export type OpenCashSessionRequest = z.infer<typeof openCashSessionRequestSchema>;
export type OpenCashSessionResponse = z.infer<typeof openCashSessionResponseSchema>;
export type CashExpectedAmountDto = z.infer<typeof cashExpectedAmountSchema>;
export type GetCurrentCashSessionResponse = z.infer<typeof getCurrentCashSessionResponseSchema>;
export type CloseCashSessionRequest = z.infer<typeof closeCashSessionRequestSchema>;
export type CloseCashSessionResponse = z.infer<typeof closeCashSessionResponseSchema>;
export type OpenTableSessionInfo = z.infer<typeof openTableSessionInfoSchema>;
export type CashMovementType = z.infer<typeof cashMovementTypeSchema>;
export type CashMovementDto = z.infer<typeof cashMovementSchema>;
export type RegisterCashMovementRequest = z.infer<typeof registerCashMovementRequestSchema>;
export type RegisterCashMovementResponse = z.infer<typeof registerCashMovementResponseSchema>;
export type ListCashMovementsResponse = z.infer<typeof listCashMovementsResponseSchema>;
