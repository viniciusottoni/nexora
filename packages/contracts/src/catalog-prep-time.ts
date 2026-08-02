import { z } from 'zod';

/**
 * US-016 (Tempo de preparo e praça por produto) — contratos de
 * `PATCH /v1/catalog/variants/{id}/prep-time`, `PATCH /v1/catalog/products/{id}/station` e
 * `GET /v1/catalog/variants/{id}/prep-time-analysis`.
 *
 * NOTA: os campos decimais (`actualAvgMinutes`/`actualP90Minutes`) são tratados aqui como
 * `z.number()`, não como string — diferente da convenção monetária do ADR-017 (`decimal` ->
 * string via `JsonConverter<decimal>` dedicado). Verificado no código real desta tarefa: esse
 * conversor ainda NÃO existe em nenhum lugar do backend (nenhuma US anterior o registrou), então
 * `System.Text.Json` serializa `decimal` como número JSON nativo hoje. Se uma US futura registrar
 * o conversor global do ADR-017, ele provavelmente vai capturar TODO `decimal` (não só dinheiro) —
 * neste caso, troque os dois campos abaixo para `z.string()` (ou um schema que aceite os dois
 * formatos durante a transição).
 */

export const updatePrepTimeThresholdsRequestSchema = z
  .object({
    prepMinutes: z.number().int().min(0, 'O tempo de preparo não pode ser negativo.'),
    warnMinutes: z.number().int().min(0).nullable(),
    criticalMinutes: z.number().int().min(0).nullable(),
  })
  .refine((value) => value.warnMinutes === null || value.warnMinutes >= value.prepMinutes, {
    message: 'O limiar de atenção não pode ser menor que o tempo de preparo.',
    path: ['warnMinutes'],
  })
  .refine(
    (value) => {
      if (value.criticalMinutes === null) return true;
      const floor = value.warnMinutes ?? value.prepMinutes;
      return value.criticalMinutes >= floor;
    },
    {
      message: 'O limiar crítico não pode ser menor que o limiar de atenção.',
      path: ['criticalMinutes'],
    },
  );

export const variantPrepTimeResponseSchema = z.object({
  variantId: z.string().uuid(),
  prepMinutes: z.number().int(),
  warnMinutes: z.number().int().nullable(),
  criticalMinutes: z.number().int().nullable(),
});

export const reassignStationRequestSchema = z.object({
  stationId: z.string().uuid().nullable(),
});

export const productStationResponseSchema = z.object({
  productId: z.string().uuid(),
  stationId: z.string().uuid().nullable(),
  stationCode: z.string().nullable(),
  stationName: z.string().nullable(),
});

export const prepTimeAnalysisResponseSchema = z.object({
  variantId: z.string().uuid(),
  configuredMinutes: z.number().int(),
  effectiveWarnMinutes: z.number().int(),
  warnMinutesInherited: z.boolean(),
  effectiveCriticalMinutes: z.number().int(),
  criticalMinutesInherited: z.boolean(),
  actualAvgMinutes: z.number().nullable(),
  actualP90Minutes: z.number().nullable(),
  sampleSize: z.number().int().nonnegative(),
  suggestion: z.number().int().nullable(),
  note: z.string().nullable(),
});

export type UpdatePrepTimeThresholdsRequest = z.infer<typeof updatePrepTimeThresholdsRequestSchema>;
export type VariantPrepTimeResponse = z.infer<typeof variantPrepTimeResponseSchema>;
export type ReassignStationRequest = z.infer<typeof reassignStationRequestSchema>;
export type ProductStationResponse = z.infer<typeof productStationResponseSchema>;
export type PrepTimeAnalysisResponse = z.infer<typeof prepTimeAnalysisResponseSchema>;
