import { z } from 'zod';

/**
 * E-08 (Alertas e Notificações) — porta de `Nexora.Contracts.Alerts.AlertContracts`
 * (backend/src/Nexora.Contracts/Alerts/AlertContracts.cs). Genérico (US-080/081/082/083),
 * diferente de `waiter-alerts.ts` (específico de US-025/US-026).
 */

export const alertSeveritySchema = z.enum(['INFO', 'WARNING', 'HIGH', 'CRITICAL']);

/** Porta de `AlertResponse` — GET /v1/alerts, /v1/notifications. */
export const alertSchema = z.object({
  id: z.string(),
  type: z.string(),
  severity: alertSeveritySchema,
  entityType: z.string().nullable(),
  entityId: z.string().nullable(),
  message: z.string(),
  raisedAt: z.string(),
  acknowledgedAt: z.string().nullable(),
  acknowledgedBy: z.string().nullable(),
  resolvedAt: z.string().nullable(),
  targetRoles: z.array(z.string()),
  targetUserId: z.string().nullable(),
  groupKey: z.string().nullable(),
});

export const alertListResponseSchema = z.object({
  alerts: z.array(alertSchema),
  nextCursor: z.string().nullable(),
});

/** US-083 §7 — GET /v1/alerts?grouped=true. */
export const alertGroupSchema = z.object({
  type: z.string(),
  count: z.number().int().positive(),
  severity: alertSeveritySchema,
  message: z.string(),
  firstRaisedAt: z.string(),
  lastRaisedAt: z.string(),
  alerts: z.array(alertSchema),
});

export const alertGroupListResponseSchema = z.object({
  groups: z.array(alertGroupSchema),
});

/**
 * US-080 §7 — GET/PATCH /v1/tenant/thresholds. `cashDivergenceAlert` é string (ADR-017: dinheiro
 * nunca é `number` em JSON, mesma convenção já usada em `tables.ts`).
 */
export const tenantThresholdsSchema = z.object({
  orderWarnMinutes: z.number().int().nonnegative(),
  orderCriticalMinutes: z.number().int().nonnegative(),
  itemInWindowMinutes: z.number().int().nonnegative(),
  tableIdleMinutes: z.number().int().nonnegative(),
  cashDivergenceAlert: z.string(),
  cmvDivergencePercent: z.number(),
  syncDelayMinutes: z.number().int().nonnegative(),
  dineInPromiseMinutes: z.number().int().nonnegative(),
  deliveryPromiseMinutes: z.number().int().nonnegative(),
  avgTimeAboveTargetPercent: z.number(),
  cancellationCountThreshold: z.number().int().nonnegative(),
  cancellationWindowMinutes: z.number().int().nonnegative(),
  discountAboveThresholdPercent: z.number(),
  discountWindowMinutes: z.number().int().nonnegative(),
});

export const updateTenantThresholdsRequestSchema = tenantThresholdsSchema.partial();

/** US-082 §7 — escopo de direcionamento de um tipo de alerta. */
export const alertRoutingScopeSchema = z.enum(['TENANT', 'RESPONSIBLE', 'TABLE_OWNER', 'STATION']);

export const alertRoutingRuleSchema = z.object({
  roles: z.array(z.string()),
  scope: alertRoutingScopeSchema,
  escalateAfterSeconds: z.number().int().positive().nullable(),
  groupWindowSeconds: z.number().int().positive().nullable(),
});

/** GET /v1/tenant/alert-routing — dicionário chaveado pelo tipo de alerta (ex.: `ORDER_LATE`). */
export const alertRoutingConfigSchema = z.record(z.string(), alertRoutingRuleSchema);

export const alertRoutingRulePatchSchema = z.object({
  roles: z.array(z.string()).nullable().optional(),
  scope: alertRoutingScopeSchema.nullable().optional(),
  escalateAfterSeconds: z.number().int().positive().nullable().optional(),
  groupWindowSeconds: z.number().int().positive().nullable().optional(),
});

/** PATCH /v1/tenant/alert-routing. */
export const updateAlertRoutingRequestSchema = z.record(z.string(), alertRoutingRulePatchSchema);

/** US-081 §7 — POST /v1/notifications/subscribe (Web Push/VAPID). */
export const pushSubscriptionKeysSchema = z.object({
  p256dh: z.string(),
  auth: z.string(),
});

export const subscribePushRequestSchema = z.object({
  endpoint: z.string(),
  keys: pushSubscriptionKeysSchema,
});

export const subscribePushResponseSchema = z.object({
  subscribed: z.boolean(),
});

/** Catálogo de tipos de alerta do MVP (US-080 §2) — espelha `Nexora.Domain.Metrics.AlertTypes.EngineTypes`. */
export const alertEngineTypes = [
  'ORDER_LATE',
  'AVG_TIME_ABOVE_TARGET',
  'PRODUCT_UNAVAILABLE',
  'CASH_DIVERGENCE',
  'SYNC_DELAY',
  'CANCELLATION_ABOVE_THRESHOLD',
  'DISCOUNT_ABOVE_THRESHOLD',
] as const;

export type AlertSeverity = z.infer<typeof alertSeveritySchema>;
export type Alert = z.infer<typeof alertSchema>;
export type AlertListResponse = z.infer<typeof alertListResponseSchema>;
export type AlertGroup = z.infer<typeof alertGroupSchema>;
export type AlertGroupListResponse = z.infer<typeof alertGroupListResponseSchema>;
export type TenantThresholds = z.infer<typeof tenantThresholdsSchema>;
export type UpdateTenantThresholdsRequest = z.infer<typeof updateTenantThresholdsRequestSchema>;
export type AlertRoutingScope = z.infer<typeof alertRoutingScopeSchema>;
export type AlertRoutingRule = z.infer<typeof alertRoutingRuleSchema>;
export type AlertRoutingConfig = z.infer<typeof alertRoutingConfigSchema>;
export type AlertRoutingRulePatch = z.infer<typeof alertRoutingRulePatchSchema>;
export type UpdateAlertRoutingRequest = z.infer<typeof updateAlertRoutingRequestSchema>;
export type PushSubscriptionKeys = z.infer<typeof pushSubscriptionKeysSchema>;
export type SubscribePushRequest = z.infer<typeof subscribePushRequestSchema>;
export type SubscribePushResponse = z.infer<typeof subscribePushResponseSchema>;
