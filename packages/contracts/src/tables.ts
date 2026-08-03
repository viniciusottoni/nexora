import { z } from 'zod';

/**
 * Porta de `TableMapEntryResponse`/`TableMapResponse` (Nexora.Contracts.Tables, US-023 §7).
 * `total` é string (ADR-017: dinheiro nunca é number em JSON — perderia precisão/arredondaria
 * silenciosamente). `status` reflete o estado combinado mesa+sessão do backend (não o vocabulário
 * de `TableCard`/`StatusPill`, que tem "OPEN" em vez de "OCCUPIED" — a tradução é feita na tela,
 * ver `table-map-page.tsx`, porque o contrato §7 da história usa "OCCUPIED" literalmente).
 */
export const tableMapStatusSchema = z.enum(['FREE', 'OCCUPIED', 'BILL_REQUESTED', 'RESERVED', 'BLOCKED']);

export const tableMapWaiterSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
});

export const tableMapSessionSchema = z.object({
  openedAt: z.string().datetime({ offset: true }),
  minutesOpen: z.number().int().nonnegative(),
  total: z.string(),
  guestCount: z.number().int().positive(),
  waiter: tableMapWaiterSchema.nullable(),
  /** US-025/US-026: id da sessão — necessário para o garçom confirmar atendimento/pedir a conta direto do mapa. */
  sessionId: z.string().uuid(),
});

export const tableMapFlagsSchema = z.object({
  waiterCalled: z.boolean(),
  billRequested: z.boolean(),
  itemsReadyToServe: z.number().int().nonnegative(),
  aboveAvgDuration: z.boolean(),
});

export const tableMapEntrySchema = z.object({
  id: z.string().uuid(),
  label: z.string().min(1),
  area: z.string().min(1),
  status: tableMapStatusSchema,
  seats: z.number().int().positive(),
  session: tableMapSessionSchema.nullable(),
  flags: tableMapFlagsSchema,
});

export const tableMapResponseSchema = z.object({
  tables: z.array(tableMapEntrySchema),
});

export type TableMapStatus = z.infer<typeof tableMapStatusSchema>;
export type TableMapWaiter = z.infer<typeof tableMapWaiterSchema>;
export type TableMapSession = z.infer<typeof tableMapSessionSchema>;
export type TableMapFlags = z.infer<typeof tableMapFlagsSchema>;
export type TableMapEntry = z.infer<typeof tableMapEntrySchema>;
export type TableMapResponse = z.infer<typeof tableMapResponseSchema>;

/** Espelha `TableMapSortBy` (Nexora.Application) — usado como query string em `GET /v1/tables`. */
export const tableMapSortBySchema = z.enum(['urgency', 'label']);
export type TableMapSortBy = z.infer<typeof tableMapSortBySchema>;
