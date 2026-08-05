import { z } from 'zod';

/**
 * Catálogo de modelos de negócio (US-142) — espelha `Nexora.Contracts.Platform.*` (backend .NET).
 * `configJson`/`seedsJson` continuam como JSON bruto (a mesma forma persistida em
 * `business_template.config`/`.seeds`): o front decide como apresentar/editar (ver
 * `apps/web-platform/src/features/business-templates`), sem este contrato precisar conhecer a
 * forma interna de cada seção (branding/operation/thresholds/... e roles/stations/...).
 */
export const businessTemplateSummarySchema = z.object({
  code: z.string().min(1),
  name: z.string().min(1),
  version: z.number().int().positive(),
});

/** Porta de `GET /v1/platform/templates` (US-142 §7). */
export const businessTemplateListResponseSchema = z.object({
  data: z.array(businessTemplateSummarySchema),
});

/** Porta de `GET /v1/platform/templates/{code}` (US-142 §7) — também o formato aceito de volta por `PUT` na mesma rota. */
export const businessTemplateDetailResponseSchema = z.object({
  code: z.string().min(1),
  name: z.string().min(1),
  version: z.number().int().positive(),
  isActive: z.boolean(),
  configJson: z.string().min(1),
  seedsJson: z.string().min(1),
  createdAt: z.string().datetime({ offset: true }),
  updatedAt: z.string().datetime({ offset: true }),
});

/** Corpo de `PUT /v1/platform/templates/{code}` (US-142 §4, cenário "Atualização de modelo"). */
export const updateBusinessTemplateRequestSchema = z.object({
  name: z.string().trim().min(1).max(120),
  configJson: z.string().min(1),
  seedsJson: z.string().min(1),
});

export type BusinessTemplateSummary = z.infer<typeof businessTemplateSummarySchema>;
export type BusinessTemplateListResponse = z.infer<typeof businessTemplateListResponseSchema>;
export type BusinessTemplateDetailResponse = z.infer<typeof businessTemplateDetailResponseSchema>;
export type UpdateBusinessTemplateRequest = z.infer<typeof updateBusinessTemplateRequestSchema>;
