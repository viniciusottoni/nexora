import { z } from 'zod';

const idSchema = z.string().uuid();

/** US-145 §7 — corpo de `POST /v1/platform/tenants/{id}/support-access`. */
export const grantSupportAccessRequestSchema = z.object({
  reason: z.string().trim().min(1, 'Informe o motivo do acesso de suporte.').max(500),
  durationMinutes: z.number().int().min(1).max(480),
});

export const grantSupportAccessResponseSchema = z.object({
  token: z.string().min(16),
  expiresAt: z.string().datetime({ offset: true }),
  notifiedCustomer: z.boolean(),
});

/** Uma linha do histórico (cliente) ou do relatório (plataforma) — mesmo formato nos dois. */
export const supportAccessSummarySchema = z.object({
  id: idSchema,
  tenantId: idSchema,
  tenantName: z.string().nullable(),
  grantedTo: idSchema.nullable(),
  reason: z.string().min(1),
  durationMinutes: z.number().int().positive(),
  grantedAt: z.string().datetime({ offset: true }),
  expiresAt: z.string().datetime({ offset: true }),
  revokedAt: z.string().datetime({ offset: true }).nullable(),
  revokedBy: idSchema.nullable(),
  lastUsedAt: z.string().datetime({ offset: true }).nullable(),
  isActive: z.boolean(),
});

export const supportAccessListResponseSchema = z.object({
  data: z.array(supportAccessSummarySchema),
});

export type GrantSupportAccessRequest = z.infer<typeof grantSupportAccessRequestSchema>;
export type GrantSupportAccessResponse = z.infer<typeof grantSupportAccessResponseSchema>;
export type SupportAccessSummary = z.infer<typeof supportAccessSummarySchema>;
export type SupportAccessListResponse = z.infer<typeof supportAccessListResponseSchema>;
