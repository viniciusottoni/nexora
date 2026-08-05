import { z } from 'zod';

/**
 * US-140 (Painel de instalações com saúde) — porta de
 * `Nexora.Contracts.Platform.InstallationHealthContracts`
 * (backend/src/Nexora.Contracts/Platform/InstallationHealthContracts.cs). Rota `/v1/platform/
 * installations/*`, distinta de `devices.ts` (dispositivos POS/KDS de UM tenant) e de
 * `tenants.ts` (provisionamento) — este é o painel de saúde do parque inteiro, RN-015: nunca
 * carrega dado de negócio do tenant, só saúde técnica.
 */

export const installationHealthSchema = z.enum(['OK', 'DEGRADED', 'DOWN']);

export const platformInstallationSummarySchema = z.object({
  installationId: z.string().uuid(),
  tenantId: z.string().uuid(),
  tenantName: z.string(),
  storeName: z.string(),
  version: z.string().nullable(),
  expectedVersion: z.string(),
  lastSeenAt: z.string().nullable(),
  syncLagSeconds: z.number().int().nonnegative().nullable(),
  pendingEvents: z.number().int().nonnegative(),
  openAlerts: z.number().int().nonnegative(),
  health: installationHealthSchema,
});

export const platformInstallationListResponseSchema = z.object({
  data: z.array(platformInstallationSummarySchema),
});

export const installationHealthCheckSchema = z.object({
  postgres: z.string().nullable(),
  redis: z.string().nullable(),
  sync: z.string().nullable(),
  checkedAt: z.string().nullable(),
});

export const installationLogEntrySchema = z.object({
  occurredAt: z.string(),
  level: z.string(),
  message: z.string(),
});

export const installationDiagnosticsResponseSchema = z.object({
  healthCheck: installationHealthCheckSchema,
  recentLogs: z.array(installationLogEntrySchema),
  diskUsagePercent: z.number().int().min(0).max(100).nullable(),
  lastBackupAt: z.string().nullable(),
});

export const installationIncidentTypeSchema = z.enum(['OFFLINE', 'DEGRADED']);

export const installationIncidentSchema = z.object({
  id: z.string().uuid(),
  type: installationIncidentTypeSchema,
  startedAt: z.string(),
  resolvedAt: z.string().nullable(),
  cause: z.string().nullable(),
  durationSeconds: z.number().int().nonnegative().nullable(),
});

export const installationIncidentListResponseSchema = z.object({
  data: z.array(installationIncidentSchema),
});

export type InstallationHealth = z.infer<typeof installationHealthSchema>;
export type PlatformInstallationSummary = z.infer<typeof platformInstallationSummarySchema>;
export type PlatformInstallationListResponse = z.infer<typeof platformInstallationListResponseSchema>;
export type InstallationHealthCheck = z.infer<typeof installationHealthCheckSchema>;
export type InstallationLogEntry = z.infer<typeof installationLogEntrySchema>;
export type InstallationDiagnosticsResponse = z.infer<typeof installationDiagnosticsResponseSchema>;
export type InstallationIncidentType = z.infer<typeof installationIncidentTypeSchema>;
export type InstallationIncident = z.infer<typeof installationIncidentSchema>;
export type InstallationIncidentListResponse = z.infer<typeof installationIncidentListResponseSchema>;
