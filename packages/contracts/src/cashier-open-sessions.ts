import { z } from 'zod';

/**
 * Porta de `OpenSessionEntryResponse`/`OpenSessionsResponse` (Nexora.Contracts.Cashier, US-050
 * §7) — painel do CAIXA sobre as sessões de mesa abertas. Não confundir com `tableMapEntrySchema`
 * (US-023, `tables.ts`): aquele é o mapa do GARÇOM (todas as mesas, livres inclusive, agrupado por
 * ambiente); este é só sessões ABERTAS, sem agrupamento, com foco em densidade e prioridade de
 * conta solicitada. `total`/`totalOpen` são string (ADR-017: dinheiro nunca é number em JSON).
 */
export const openSessionStatusSchema = z.enum(['OPEN', 'BILL_REQUESTED', 'PAID', 'CLOSED']);

export const openSessionWaiterSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
});

export const openSessionEntrySchema = z.object({
  sessionId: z.string().uuid(),
  table: z.string().min(1),
  area: z.string().min(1),
  openedAt: z.string().datetime({ offset: true }),
  minutesOpen: z.number().int().nonnegative(),
  guestCount: z.number().int().positive(),
  waiter: openSessionWaiterSchema.nullable(),
  total: z.string(),
  status: openSessionStatusSchema,
  billRequestedAt: z.string().datetime({ offset: true }).nullable(),
  /** Segundos desde que a conta foi pedida — só preenchido quando `status` é `BILL_REQUESTED`. */
  waitingSeconds: z.number().int().nonnegative().nullable(),
  /** Itens ainda não servidos (produção ou prontos aguardando entrega) — US-050 §7. */
  pendingItems: z.number().int().nonnegative(),
  /** `short_code` do pedido mais recente da sessão (ex. "A47") — identificador de "comanda" usado na busca. Nulo enquanto a sessão não tem nenhum pedido lançado. */
  orderCode: z.string().nullable(),
});

export const openSessionsSummarySchema = z.object({
  openSessions: z.number().int().nonnegative(),
  totalOpen: z.string(),
});

export const openSessionsResponseSchema = z.object({
  sessions: z.array(openSessionEntrySchema),
  summary: openSessionsSummarySchema,
});

export type OpenSessionStatus = z.infer<typeof openSessionStatusSchema>;
export type OpenSessionWaiter = z.infer<typeof openSessionWaiterSchema>;
export type OpenSessionEntry = z.infer<typeof openSessionEntrySchema>;
export type OpenSessionsSummary = z.infer<typeof openSessionsSummarySchema>;
export type OpenSessionsResponse = z.infer<typeof openSessionsResponseSchema>;

/** Espelha `GetOpenSessionsSortBy` (Nexora.Application) — usado como query string em `GET /v1/cash/open-sessions`. */
export const openSessionsSortBySchema = z.enum(['urgency', 'table']);
export type OpenSessionsSortBy = z.infer<typeof openSessionsSortBySchema>;
