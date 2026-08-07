import { z } from 'zod';

const idSchema = z.string().uuid();
export const tenantSlugSchema = z
  .string()
  .min(1, 'Informe o endereço do estabelecimento.')
  .max(64)
  .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Use apenas letras minúsculas, números e hífens.');

export const createTenantRequestSchema = z.object({
  name: z.string().trim().min(2).max(160),
  slug: tenantSlugSchema,
  plan: z
    .string()
    .trim()
    .min(1)
    .max(32)
    .regex(/^[A-Z][A-Z0-9_]*$/),
  // US-142: modelo deixou de ser um literal fixo — agora é o `code` de um `business_template`
  // ativo (pizzaria/hamburgueria/restaurante/lanchonete/...), buscado em
  // `GET /v1/platform/templates` e validado no servidor (não aqui: a lista de códigos válidos
  // muda sem deploy do front, ver ADR-013/ADR-032).
  template: z
    .string()
    .trim()
    .min(1, 'Informe o modelo de negócio.')
    .regex(/^[A-Z][A-Z0-9_]*$/, 'Código de modelo inválido.'),
  owner: z.object({
    name: z.string().trim().min(2).max(160),
    email: z.string().trim().email().max(254),
  }),
  store: z.object({
    name: z.string().trim().min(2).max(160),
    timezone: z
      .string()
      .trim()
      .min(1)
      .max(100)
      .refine((value) => {
        try {
          new Intl.DateTimeFormat('pt-BR', { timeZone: value }).format();
          return true;
        } catch {
          return false;
        }
      }, 'Informe um fuso horário IANA válido.'),
  }),
});

export const provisioningChecklistItemSchema = z.object({
  code: z.string().min(1),
  label: z.string().min(1),
  status: z.enum(['COMPLETED', 'PENDING']),
});

// US-151 (Diretório de estabelecimentos) normaliza o status em CAIXA ALTA no fio — decisão
// acordada entre as lanes de frontend e backend desta história (doc. §7: "códigos de status
// normalizados em caixa alta; valores internos do enum não podem vazar como `Trial` ou número").
// Substitui o `TenantStatus.ToString()` em PascalCase usado por `ListTenantsQueryHandler`
// (US-002/US-150) — o handler do diretório (`GET /v1/platform/tenants`, US-151) passa a
// normalizar antes de serializar.
//
// US-153 (Ciclo de vida do estabelecimento) substitui a máquina de estados legada — `TRIAL` foi
// renomeado para `PROVISIONED` e `INSTALLING` foi adicionado — pelo conjunto canônico único usado
// em todas as camadas (.NET e TypeScript, doc. §12 "Estado canônico idêntico"):
// `PROVISIONED → INSTALLING → ACTIVE ⇄ SUSPENDED → CANCELLED`.
export const tenantStatusSchema = z.enum([
  'PROVISIONED',
  'INSTALLING',
  'ACTIVE',
  'SUSPENDED',
  'CANCELLED',
]);

// US-151 §7 — saúde resumida da instalação, usada como coluna e filtro do diretório.
export const tenantHealthSchema = z.enum(['OK', 'DEGRADED', 'DOWN', 'UNKNOWN']);

// US-151 §3.1/§10 — ordenação do diretório; `attention` (criticidade) é o padrão do critério de
// aceite "Listagem inicial" ("ordenada por criticidade e atualização").
export const tenantDirectorySortSchema = z.enum(['name', 'createdAt', 'updatedAt', 'attention']);

export const tenantSummarySchema = z.object({
  id: idSchema,
  slug: tenantSlugSchema,
  name: z.string().min(1),
  plan: z.string().min(1),
  status: tenantStatusSchema,
  createdAt: z.string().datetime({ offset: true }),
});

export const createTenantResponseSchema = z.object({
  // "PROVISIONED" descreve o RESULTADO deste endpoint (US-002 §7), literal distinto do estado
  // comercial persistido (`tenantStatusSchema`) — nunca reaproveitar o mesmo enum para os dois.
  tenant: z.object({ id: idSchema, slug: tenantSlugSchema, status: z.literal('PROVISIONED') }),
  store: z.object({ id: idSchema, name: z.string().min(1) }),
  installToken: z.string().min(16),
  installCommand: z.string().startsWith('./install.sh --tenant='),
  ownerInviteSentTo: z.string().email(),
  checklist: z.array(provisioningChecklistItemSchema).length(9),
});

export const tenantListResponseSchema = z.object({ data: z.array(tenantSummarySchema) });
export const slugAvailabilityResponseSchema = z.object({
  slug: tenantSlugSchema,
  available: z.boolean(),
});

/**
 * US-151 · Diretório de estabelecimentos com busca e filtros — porta de
 * `GET /v1/platform/tenants?query=...&status=...&plan=...&template=...&health=...&createdFrom=
 * ...&createdTo=...&sort=...&limit=...&cursor=...`. Contrato acordado com a lane de backend desta
 * história (não confundir com `tenantSummarySchema`/`tenantListResponseSchema` acima, que
 * continuam servindo o diretório mínimo da US-150).
 */
export const tenantDirectoryItemSchema = z.object({
  id: idSchema,
  name: z.string().min(1),
  slug: tenantSlugSchema,
  status: tenantStatusSchema,
  plan: z.string().min(1),
  ownerEmail: z.string().email().nullable(),
  storesCount: z.number().int().nonnegative(),
  installationsCount: z.number().int().nonnegative(),
  health: tenantHealthSchema,
  createdAt: z.string().datetime({ offset: true }),
  updatedAt: z.string().datetime({ offset: true }),
});

export const tenantDirectoryAppliedFiltersSchema = z.object({
  query: z.string().nullable().optional(),
  status: z.array(tenantStatusSchema).default([]),
  plan: z.array(z.string()).default([]),
  template: z.array(z.string()).default([]),
  health: z.array(tenantHealthSchema).default([]),
  createdFrom: z.string().datetime({ offset: true }).nullable().optional(),
  createdTo: z.string().datetime({ offset: true }).nullable().optional(),
  sort: tenantDirectorySortSchema,
  limit: z.number().int().positive(),
});

export const tenantDirectoryResponseSchema = z.object({
  data: z.array(tenantDirectoryItemSchema),
  nextCursor: z.string().nullable(),
  appliedFilters: tenantDirectoryAppliedFiltersSchema,
});

/** Params de requisição (todos opcionais — quem monta a querystring aplica os defaults de sort/limit). */
export const tenantDirectoryQuerySchema = z.object({
  query: z.string().trim().max(160).optional(),
  status: z.array(tenantStatusSchema).optional(),
  plan: z.array(z.string().min(1)).optional(),
  template: z.array(z.string().min(1)).optional(),
  health: z.array(tenantHealthSchema).optional(),
  createdFrom: z.string().datetime({ offset: true }).optional(),
  createdTo: z.string().datetime({ offset: true }).optional(),
  sort: tenantDirectorySortSchema.optional(),
  limit: z.number().int().positive().max(100).optional(),
  cursor: z.string().optional(),
});

export const acceptOwnerInviteRequestSchema = z.object({
  token: z.string().min(32).max(256),
  password: z.string().min(12).max(128),
});

/**
 * US-153 · Ciclo de vida do estabelecimento — resposta de
 * `POST /v1/platform/tenants/{id}/status-transitions` (§7). `version` é o novo `statusVersion`
 * (controle de concorrência otimista, ADR-020/ADR-023) a ser usado no próximo `If-Match`.
 */
export const tenantStatusTransitionResponseSchema = z.object({
  tenantId: idSchema,
  previousStatus: tenantStatusSchema,
  status: tenantStatusSchema,
  version: z.number().int().positive(),
  changedAt: z.string().datetime({ offset: true }),
});

export type CreateTenantRequest = z.infer<typeof createTenantRequestSchema>;
export type CreateTenantResponse = z.infer<typeof createTenantResponseSchema>;
export type TenantStatus = z.infer<typeof tenantStatusSchema>;
export type TenantHealth = z.infer<typeof tenantHealthSchema>;
export type TenantDirectorySort = z.infer<typeof tenantDirectorySortSchema>;
export type TenantSummaryContract = z.infer<typeof tenantSummarySchema>;
export type TenantListResponse = z.infer<typeof tenantListResponseSchema>;
export type SlugAvailabilityResponse = z.infer<typeof slugAvailabilityResponseSchema>;
export type TenantDirectoryItem = z.infer<typeof tenantDirectoryItemSchema>;
export type TenantDirectoryAppliedFilters = z.infer<typeof tenantDirectoryAppliedFiltersSchema>;
export type TenantDirectoryResponse = z.infer<typeof tenantDirectoryResponseSchema>;
export type TenantDirectoryQuery = z.infer<typeof tenantDirectoryQuerySchema>;
export type TenantStatusTransitionResponse = z.infer<typeof tenantStatusTransitionResponseSchema>;
