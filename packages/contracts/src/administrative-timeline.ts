import { z } from 'zod';

/**
 * US-157 · Central operacional, auditoria e atalhos de suporte — espelha
 * `Nexora.Contracts.Tenants.AdministrativeTimelineContracts` (C#).
 * `GET /v1/platform/tenants/{id}/administrative-timeline`. `summary` é uma frase pronta em
 * português (mesma convenção de `AuditLogEntry.summary` em `audit.ts`) — a UI nunca renderiza JSON
 * bruto ao administrador.
 */
export const administrativeTimelineEntryTypeSchema = z.enum([
  'CREATION',
  'STATUS_CHANGED',
  'PLAN_CHANGED',
  'OWNER_CHANGED',
  'CREDENTIALS_REISSUED',
  'DOMAIN_REGISTERED',
  'SUPPORT_GRANTED',
  'INCIDENT',
]);

export const administrativeTimelineActorSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
});

export const administrativeTimelineEntrySchema = z.object({
  type: administrativeTimelineEntryTypeSchema,
  occurredAt: z.string().datetime({ offset: true }),
  actor: administrativeTimelineActorSchema.nullable(),
  origin: z.string().min(1),
  reason: z.string().min(1),
  correlationId: z.string().nullable().optional(),
  summary: z.string().min(1),
});

export const administrativeTimelineListResponseSchema = z.object({
  data: z.array(administrativeTimelineEntrySchema),
  nextCursor: z.string().nullable(),
});

export type AdministrativeTimelineEntryType = z.infer<typeof administrativeTimelineEntryTypeSchema>;
export type AdministrativeTimelineActor = z.infer<typeof administrativeTimelineActorSchema>;
export type AdministrativeTimelineEntry = z.infer<typeof administrativeTimelineEntrySchema>;
export type AdministrativeTimelineListResponse = z.infer<
  typeof administrativeTimelineListResponseSchema
>;

/** Filtros de consulta — mesma convenção de `AuditLogFilters` (audit.ts): shape puro, não é corpo de escrita. */
export interface AdministrativeTimelineFilters {
  readonly type?: readonly AdministrativeTimelineEntryType[];
  readonly from?: string;
  readonly to?: string;
  readonly actorId?: string;
  readonly correlationId?: string;
  readonly limit?: number;
  readonly cursor?: string;
}
