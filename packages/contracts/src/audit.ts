import { z } from 'zod';

/**
 * US-091 (Consulta e filtro da trilha) — contrato de `GET /v1/audit` (Nexora.Api.Cloud, policy
 * `AuditRead`). Espelha exatamente a forma escrita à mão pelo backend (`Nexora.Contracts`, camada
 * `Api.Cloud`), no mesmo estilo de `roles.ts`: schema Zod da RESPOSTA (validada em runtime, porque
 * vem de rede) e um tipo simples para os FILTROS de request — que não é corpo de escrita nem
 * precisa de validação Zod, é só o shape que `AuditApi.list()` usa para montar a querystring.
 *
 * `before`/`after` são JSON livre (o formato interno de cada `action`, ex. `{ discount: 1980 }`
 * para `DISCOUNT_APPLIED`) — a UI NUNCA deve renderizar isso como bloco de JSON bruto (Gherkin da
 * US: "não deve exibir JSON bruto ao gestor"); ela itera as chaves e mostra como par rótulo/valor.
 * O texto pronto para exibição em português é sempre `summary`.
 */

/** Ator/autorizador de uma entrada — sempre `id` + `name` de `app_user`, ou `null` quando o evento não teve autor humano (ex. processo automático). */
export const auditActorRefSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
});

/** Dispositivo de origem (`device.label`) — `null` quando a ação não veio de um dispositivo físico (ex. consulta feita pela nuvem). */
export const auditDeviceRefSchema = z.object({
  id: z.string().uuid(),
  label: z.string().min(1),
});

/** Ligação com a entidade de origem (pedido, item, mesa, preço...) — usada para "chegar ao pedido de origem" (US-091 §4, cenário "Navegação até a origem"). */
export const auditTargetRefSchema = z.object({
  type: z.string().min(1),
  id: z.string().uuid(),
  label: z.string().min(1),
});

export const auditLogEntrySchema = z.object({
  id: z.string().uuid(),
  action: z.string().min(1),
  /** Frase pronta em português — é isto que a tela mostra como texto principal da linha, nunca `before`/`after` cru. */
  summary: z.string().min(1),
  actor: auditActorRefSchema.nullable(),
  authorizedBy: auditActorRefSchema.nullable(),
  device: auditDeviceRefSchema.nullable(),
  occurredAt: z.string().datetime({ offset: true }),
  target: auditTargetRefSchema.nullable(),
  /** Estado anterior, em JSON livre — nunca renderizar diretamente, sempre iterar chave/valor. */
  before: z.record(z.string(), z.unknown()).nullable().optional(),
  /** Estado novo, em JSON livre — mesma regra de `before`. */
  after: z.record(z.string(), z.unknown()).nullable().optional(),
  reason: z.string().nullable().optional(),
  traceId: z
    .string()
    .regex(/^[0-9a-f]{32}$/)
    .nullable()
    .optional(),
});

export const auditLogListMetaSchema = z.object({
  nextCursor: z.string().nullable(),
  hasMore: z.boolean(),
});

export const auditLogListResponseSchema = z.object({
  data: z.array(auditLogEntrySchema),
  meta: auditLogListMetaSchema,
});

export type AuditActorRef = z.infer<typeof auditActorRefSchema>;
export type AuditDeviceRef = z.infer<typeof auditDeviceRefSchema>;
export type AuditTargetRef = z.infer<typeof auditTargetRefSchema>;
export type AuditLogEntry = z.infer<typeof auditLogEntrySchema>;
export type AuditLogListMeta = z.infer<typeof auditLogListMetaSchema>;
export type AuditLogListResponse = z.infer<typeof auditLogListResponseSchema>;

/**
 * Filtros de consulta — todos opcionais, viram query string em `AuditApi.list()`. Não é corpo de
 * escrita (não há POST/PUT aqui), então não precisa de schema Zod: é só o shape que o cliente HTTP
 * monta. `minAmount` é string decimal (ADR-017 — dinheiro nunca é `number` no contrato, mesmo em
 * query string, para não sugerir que o valor tolera perda de precisão de ponto flutuante).
 */
export interface AuditLogFilters {
  readonly from?: string;
  readonly to?: string;
  readonly actorId?: string;
  readonly authorizedBy?: string;
  readonly entityId?: string;
  readonly action?: string;
  readonly entity?: string;
  readonly minAmount?: string;
  readonly limit?: number;
  readonly cursor?: string;
}
