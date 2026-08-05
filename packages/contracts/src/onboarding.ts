import { z } from 'zod';

/**
 * Roteiro de implantação autoatendido (US-141) — as nove chaves e três status na EXATA
 * grafia SCREAMING_SNAKE_CASE do contrato (US-141 §7). Espelha
 * `Nexora.Contracts.Platform.OnboardingStepKeyWireFormat`/`OnboardingStepKey` (backend .NET) — as
 * duas listas precisam continuar em sincronia.
 */
export const onboardingStepKeySchema = z.enum([
  'TENANT_CREATED',
  'BRANDING',
  'MENU',
  'TABLES',
  'EDGE_INSTALL',
  'PAYMENT_CONFIG',
  'TRAINING',
  'PILOT',
  'ACTIVATION',
]);

export const onboardingStepStatusSchema = z.enum(['PENDING', 'IN_PROGRESS', 'DONE']);

export const onboardingStepProgressSchema = z.object({
  products: z.number().int().nonnegative().nullable(),
  expected: z.number().int().nonnegative().nullable(),
});

export const onboardingStepSchema = z.object({
  key: onboardingStepKeySchema,
  status: onboardingStepStatusSchema,
  progress: onboardingStepProgressSchema.nullable().optional(),
});

/** Porta de `GET /v1/platform/tenants/{id}/onboarding` (US-141 §7). */
export const onboardingStatusResponseSchema = z.object({
  steps: z.array(onboardingStepSchema).length(9),
  startedAt: z.string().datetime({ offset: true }).nullable(),
  elapsedBusinessDays: z.number().int().nonnegative().nullable(),
});

/**
 * `POST /v1/platform/tenants/{id}/activate` — 422 `ONBOARDING_INCOMPLETE`, `meta.pendingItems`
 * (US-141 §7 documenta `meta.pending`; o backend hoje reaproveita o mesmo mecanismo genérico de
 * `PendingItemsClosePolicy`, que produz `meta.pendingItems` — ver o relatório da tarefa que
 * introduziu este contrato para o desvio documentado). O front lê `pendingItems` com fallback para
 * `pending`, para já ficar correto se/quando o backend for ajustado ao nome exato do contrato.
 */
export const onboardingActivationPendingMetaSchema = z.object({
  pendingItems: z.array(onboardingStepKeySchema).optional(),
  pending: z.array(onboardingStepKeySchema).optional(),
});

export type OnboardingStepKey = z.infer<typeof onboardingStepKeySchema>;
export type OnboardingStepStatus = z.infer<typeof onboardingStepStatusSchema>;
export type OnboardingStep = z.infer<typeof onboardingStepSchema>;
export type OnboardingStatusResponse = z.infer<typeof onboardingStatusResponseSchema>;
export type OnboardingActivationPendingMeta = z.infer<typeof onboardingActivationPendingMetaSchema>;

/** Rótulo de negócio de cada passo (US-141 §10 "linguagem de negócio" no painel do cliente). */
export const ONBOARDING_STEP_LABELS: Record<OnboardingStepKey, string> = {
  TENANT_CREATED: 'Estabelecimento criado',
  BRANDING: 'Identidade visual',
  MENU: 'Cardápio',
  TABLES: 'Mesas',
  EDGE_INSTALL: 'Servidor local instalado',
  PAYMENT_CONFIG: 'Meios de pagamento',
  TRAINING: 'Treinamento da equipe',
  PILOT: 'Piloto acompanhado',
  ACTIVATION: 'Ativação',
};

/** Ordem de exibição do checklist — a mesma ordem dos nove passos da Visão Geral (8.5)/US-141 §7. */
export const ONBOARDING_STEP_ORDER: readonly OnboardingStepKey[] = [
  'TENANT_CREATED',
  'BRANDING',
  'MENU',
  'TABLES',
  'EDGE_INSTALL',
  'PAYMENT_CONFIG',
  'TRAINING',
  'PILOT',
  'ACTIVATION',
];
