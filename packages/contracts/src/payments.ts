import { z } from 'zod';

/**
 * Contratos de US-052 (Múltiplas formas de pagamento), US-058 (Pagamento de maquininha externa),
 * US-054 (Desconto com autorização), US-053 (Taxa de serviço com retirada registrada) e US-057
 * (Comprovante não fiscal) — porta de `Nexora.Contracts.Cashier.*` (backend). Dinheiro sempre como
 * string (ADR-017), mesma convenção de `billing.ts`.
 */
const moneyStringSchema = z.string().regex(/^-?\d+(\.\d+)?$/, 'Valor monetário inválido');

const paymentMethodSchema = z.enum(['CASH', 'CREDIT', 'DEBIT', 'PIX', 'ONLINE', 'VOUCHER', 'OTHER']);

/** Um item do array de `POST /v1/sessions/{id}/payments` — US-052 §7 / US-058 §7. */
export const paymentRequestSchema = z.object({
  method: paymentMethodSchema,
  amount: z.number().positive(),
  receivedAmount: z.number().positive().nullable().optional(),
  provider: z.string().nullable().optional(),
  providerRef: z.string().nullable().optional(),
  brand: z.string().nullable().optional(),
  installments: z.number().int().positive().optional(),
  confirmDuplicate: z.boolean().optional(),
});

export const registerPaymentsRequestSchema = z.object({
  payments: z.array(paymentRequestSchema).min(1),
});

const reconciliationStatusSchema = z.enum(['NOTAPPLICABLE', 'PENDING', 'RECONCILED']);

export const registeredPaymentSchema = z.object({
  id: z.string().uuid(),
  method: paymentMethodSchema,
  amount: moneyStringSchema,
  netAmount: moneyStringSchema,
  feeAmount: moneyStringSchema,
  changeAmount: moneyStringSchema,
  provider: z.string().nullable(),
  providerRef: z.string().nullable(),
  reconciliationStatus: reconciliationStatusSchema,
});

export const registerPaymentsResponseSchema = z.object({
  session: z.object({ status: z.string() }),
  payments: z.array(registeredPaymentSchema),
  change: moneyStringSchema,
  receipt: z.object({ url: z.string() }),
});

/** `POST /v1/sessions/{id}/discount` (US-054). */
export const applyDiscountRequestSchema = z
  .object({
    percent: z.number().min(0).max(100).nullable().optional(),
    amount: z.number().min(0).nullable().optional(),
    reason: z.string().min(1),
    scope: z.enum(['SESSION', 'ITEM']),
    orderItemId: z.string().uuid().nullable().optional(),
  })
  .refine((v) => {
    const hasPercent = v.percent !== null && v.percent !== undefined;
    const hasAmount = v.amount !== null && v.amount !== undefined;
    return hasPercent !== hasAmount;
  }, {
    message: 'Informe apenas o percentual ou o valor do desconto.',
  });

export const applyDiscountResponseSchema = z.object({
  session: z.object({
    discount: moneyStringSchema,
    discountPercent: z.number(),
    total: moneyStringSchema,
  }),
  authorizedBy: z.object({ id: z.string().uuid(), name: z.string() }).nullable(),
});

/** `POST /v1/sessions/{id}/service-fee/waive` (US-053) — registro AUTORITATIVO, distinto da retirada efêmera por pessoa de US-027. */
export const waiveSessionServiceFeeRequestSchema = z.object({
  reason: z.string().min(1),
  scope: z.enum(['FULL', 'PARTIAL']),
  person: z.number().int().positive().nullable().optional(),
});

export const waiveSessionServiceFeeResponseSchema = z.object({
  session: z.object({
    serviceFee: moneyStringSchema,
    total: moneyStringSchema,
  }),
});

/** `GET /v1/sessions/{id}/receipt` (US-057) — `isFiscal` é sempre `false` (RN-023, pendência crítica). */
export const receiptPaymentSchema = z.object({
  method: z.string(),
  amount: moneyStringSchema,
});

export const receiptSchema = z.object({
  url: z.string(),
  number: z.string(),
  isFiscal: z.boolean(),
  issuedAt: z.string(),
  items: z.array(z.unknown()),
  payments: z.array(receiptPaymentSchema),
  subtotal: moneyStringSchema,
  serviceFee: moneyStringSchema,
  discount: moneyStringSchema,
  total: moneyStringSchema,
});

export const getReceiptResponseSchema = z.object({ receipt: receiptSchema });

export const printReceiptResponseSchema = z.object({ queued: z.boolean() });

export type PaymentRequest = z.infer<typeof paymentRequestSchema>;
export type RegisterPaymentsRequest = z.infer<typeof registerPaymentsRequestSchema>;
export type RegisteredPaymentDto = z.infer<typeof registeredPaymentSchema>;
export type RegisterPaymentsResponse = z.infer<typeof registerPaymentsResponseSchema>;
export type ApplyDiscountRequest = z.infer<typeof applyDiscountRequestSchema>;
export type ApplyDiscountResponse = z.infer<typeof applyDiscountResponseSchema>;
export type WaiveSessionServiceFeeRequest = z.infer<typeof waiveSessionServiceFeeRequestSchema>;
export type WaiveSessionServiceFeeResponse = z.infer<typeof waiveSessionServiceFeeResponseSchema>;
export type ReceiptDto = z.infer<typeof receiptSchema>;
export type GetReceiptResponse = z.infer<typeof getReceiptResponseSchema>;
export type PrintReceiptResponse = z.infer<typeof printReceiptResponseSchema>;
