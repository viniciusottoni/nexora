import { z } from 'zod';

const idSchema = z.string().uuid();

/**
 * US-154 · Gestão de planos e configuração comercial — espelha
 * `Nexora.Contracts.Tenants.TenantPlanContracts` (C#). Arquivo NOVO, ainda não exportado por
 * `packages/contracts/src/index.ts` (integração central posterior adiciona
 * `export * from './tenant-plan.js';`) — ver relatório final da tarefa. Enquanto isso, nenhum app
 * consegue importar estes tipos via `@nexora/contracts`; `tenant-plan-section.tsx` define seus
 * próprios tipos locais equivalentes até a integração acontecer.
 */
export const platformPlanSummarySchema = z.object({
  code: z.string().min(1),
  name: z.string().min(1),
  active: z.boolean(),
  capabilities: z.array(z.string()),
});

export const platformPlanListResponseSchema = z.object({
  data: z.array(platformPlanSummarySchema),
});

export const tenantPlanScheduledSchema = z.object({
  plan: z.string().min(1),
  effectiveAt: z.string().datetime({ offset: true }),
});

export const tenantPlanHistoryEntrySchema = z.object({
  previous: z.string().min(1),
  next: z.string().min(1),
  requestedAt: z.string().datetime({ offset: true }),
  effectiveAt: z.string().datetime({ offset: true }),
  appliedAt: z.string().datetime({ offset: true }).nullable(),
  reason: z.string().min(1),
  actorId: idSchema.nullable(),
});

export const tenantPlanResponseSchema = z.object({
  current: z.string().min(1),
  effectiveCapabilities: z.array(z.string()),
  scheduled: tenantPlanScheduledSchema.nullable(),
  consistent: z.boolean(),
  version: z.number().int().positive(),
  history: z.array(tenantPlanHistoryEntrySchema),
});

export const tenantPlanUpdateRequestSchema = z.object({
  plan: z
    .string()
    .trim()
    .min(1, 'Informe o plano.')
    .regex(/^[A-Z][A-Z0-9_]*$/, 'Código de plano inválido.'),
  effectiveAt: z.string().datetime({ offset: true }).optional(),
  reason: z.string().trim().min(1, 'O motivo é obrigatório.'),
});

export const tenantPlanUpdateResponseSchema = z.object({
  current: z.string().min(1),
  scheduled: tenantPlanScheduledSchema.nullable(),
  version: z.number().int().positive(),
});

/**
 * Endpoint ADICIONAL ao contrato abreviado do §7 da US-154 —
 * `POST /v1/platform/tenants/{id}/plan/reconciliations` (ver docstring de
 * `ReconcileTenantPlanConfigCommand` no backend para a justificativa).
 */
export const tenantPlanReconcileResponseSchema = z.object({
  current: z.string().min(1),
  effectiveCapabilities: z.array(z.string()),
  consistent: z.boolean(),
  changed: z.boolean(),
});

export type PlatformPlanSummary = z.infer<typeof platformPlanSummarySchema>;
export type PlatformPlanListResponse = z.infer<typeof platformPlanListResponseSchema>;
export type TenantPlanScheduled = z.infer<typeof tenantPlanScheduledSchema>;
export type TenantPlanHistoryEntry = z.infer<typeof tenantPlanHistoryEntrySchema>;
export type TenantPlanResponse = z.infer<typeof tenantPlanResponseSchema>;
export type TenantPlanUpdateRequest = z.infer<typeof tenantPlanUpdateRequestSchema>;
export type TenantPlanUpdateResponse = z.infer<typeof tenantPlanUpdateResponseSchema>;
export type TenantPlanReconcileResponse = z.infer<typeof tenantPlanReconcileResponseSchema>;

/** Referência exclusivamente de leitura/documentação — não usado diretamente pelo id de tenant nas rotas (a rota já tipa `id` como string). */
export const tenantIdSchema = idSchema;
