import { z } from 'zod';

/**
 * US-150 (Estrutura e navegação do painel de plataforma) — porta de
 * `Nexora.Contracts.Platform.PlatformSummaryContracts`
 * (backend/src/Nexora.Contracts/Platform/PlatformSummaryContracts.cs). Rota `GET /v1/platform/
 * summary`, consumida pela visão geral (raiz) do `web-platform`. Deliberadamente pequeno — só
 * contadores, nunca linha por estabelecimento/instalação (US-150 §15).
 */

export const platformTenantsSummarySchema = z.object({
  total: z.number().int().nonnegative(),
  active: z.number().int().nonnegative(),
  attention: z.number().int().nonnegative(),
});

export const platformInstallationsSummarySchema = z.object({
  healthy: z.number().int().nonnegative(),
  degraded: z.number().int().nonnegative(),
  offline: z.number().int().nonnegative(),
});

export const platformSummaryResponseSchema = z.object({
  tenants: platformTenantsSummarySchema,
  installations: platformInstallationsSummarySchema,
  pendingInvites: z.number().int().nonnegative(),
  generatedAt: z.string(),
});

export type PlatformTenantsSummary = z.infer<typeof platformTenantsSummarySchema>;
export type PlatformInstallationsSummary = z.infer<typeof platformInstallationsSummarySchema>;
export type PlatformSummaryResponse = z.infer<typeof platformSummaryResponseSchema>;
