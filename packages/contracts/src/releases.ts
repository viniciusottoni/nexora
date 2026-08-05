import { z } from 'zod';

/**
 * US-146 (Atualização controlada do parque) — porta de
 * `Nexora.Contracts.Platform.ReleaseContracts` (backend/src/Nexora.Contracts/Platform/ReleaseContracts.cs).
 * Rota `/v1/platform/releases/*` — publicação de versão de edge com liberação gradual e o
 * progresso de rollout de cada versão no parque. ADR-019: a atualização é PUXADA pelo edge; esta
 * API só declara o que está disponível, nunca empurra nada para uma instalação.
 */

export const publishReleaseRequestSchema = z.object({
  version: z.string().min(1).max(20),
  rolloutPercent: z.number().int().min(0).max(100),
  notes: z.string().max(2000).nullable().optional(),
});

export const releaseSchema = z.object({
  id: z.string().uuid(),
  version: z.string(),
  rolloutPercent: z.number().int().min(0).max(100),
  notes: z.string().nullable(),
  publishedAt: z.string(),
  publishedBy: z.string().uuid().nullable(),
});

export const publishReleaseResponseSchema = z.object({
  release: releaseSchema,
});

export const releaseRolloutResponseSchema = z.object({
  total: z.number().int().nonnegative(),
  updated: z.number().int().nonnegative(),
  failed: z.number().int().nonnegative(),
  pending: z.number().int().nonnegative(),
});

export type PublishReleaseRequest = z.infer<typeof publishReleaseRequestSchema>;
export type Release = z.infer<typeof releaseSchema>;
export type PublishReleaseResponse = z.infer<typeof publishReleaseResponseSchema>;
export type ReleaseRolloutResponse = z.infer<typeof releaseRolloutResponseSchema>;
