import { z } from 'zod';

/** Porta de <c>AreaResponse</c> (Nexora.Contracts.Operation) — US-020. */
export const areaSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  position: z.number().int(),
  active: z.boolean(),
  tableCount: z.number().int().nonnegative(),
});

export const areaListResponseSchema = z.object({
  items: z.array(areaSchema),
});

export const createAreaRequestSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome do ambiente').max(100),
  position: z.number().int().default(0),
});

export const updateAreaRequestSchema = createAreaRequestSchema;

export type AreaDto = z.infer<typeof areaSchema>;
export type AreaListResponse = z.infer<typeof areaListResponseSchema>;
export type CreateAreaRequest = z.infer<typeof createAreaRequestSchema>;
export type UpdateAreaRequest = z.infer<typeof updateAreaRequestSchema>;
