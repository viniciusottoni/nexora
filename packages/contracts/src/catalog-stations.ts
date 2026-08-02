import { z } from 'zod';

/**
 * US-017 (Cadastro de praças de produção) — porta de
 * `Nexora.Contracts.Stations.*` (backend/src/Nexora.Contracts/Stations/*.cs).
 * Cor validada como CHAVE semântica (mesma regra do `CreateStationCommandValidator`), nunca
 * hexadecimal literal (ADR-010) — o mapeamento chave → token CSS real vive em
 * `apps/web-admin/src/stations/stations-api.ts` (`STATION_COLOR_CSS_VAR`, derivado de
 * `packages/ui/src/tokens/colors.css`), não aqui.
 */
const stationColorKeySchema = z
  .string()
  .regex(/^[a-z][a-z-]*$/, 'Escolha uma cor da paleta permitida');

export const createStationRequestSchema = z.object({
  code: z
    .string()
    .trim()
    .min(2, 'Informe um código')
    .max(32)
    .regex(/^[A-Z][A-Z0-9_]*$/, 'Use letras maiúsculas, números e sublinhado'),
  name: z.string().trim().min(2, 'Informe um nome').max(100),
  color: stationColorKeySchema.optional(),
  capacitySlots: z
    .number()
    .int('Use um número inteiro')
    .positive('A capacidade deve ser maior que zero')
    .optional(),
  isBottleneck: z.boolean().default(false),
  position: z.number().int().nonnegative().default(0),
});

export const updateStationRequestSchema = z
  .object({
    name: z.string().trim().min(2, 'Informe um nome').max(100).optional(),
    color: stationColorKeySchema.optional(),
    capacitySlots: z.number().int().positive('A capacidade deve ser maior que zero').optional(),
    isBottleneck: z.boolean().optional(),
    position: z.number().int().nonnegative().optional(),
  })
  .refine(
    (value) =>
      value.name !== undefined ||
      value.color !== undefined ||
      value.capacitySlots !== undefined ||
      value.isBottleneck !== undefined ||
      value.position !== undefined,
    { message: 'Informe ao menos uma alteração' },
  );

export const stationSchema = z.object({
  id: z.string().uuid(),
  code: z.string().min(1),
  name: z.string().min(1),
  color: z.string().nullable(),
  capacitySlots: z.number().int().nullable(),
  isBottleneck: z.boolean(),
  position: z.number().int(),
  isActive: z.boolean(),
  /** Contagem de produtos vinculados — US-017 §10, usada para bloquear exclusão na UI. */
  linkedProductCount: z.number().int().nonnegative(),
});

export const stationListResponseSchema = z.object({
  items: z.array(stationSchema),
});

export type CreateStationRequest = z.infer<typeof createStationRequestSchema>;
export type UpdateStationRequest = z.infer<typeof updateStationRequestSchema>;
export type StationDto = z.infer<typeof stationSchema>;
export type StationListResponse = z.infer<typeof stationListResponseSchema>;
