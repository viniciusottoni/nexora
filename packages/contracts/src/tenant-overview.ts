import { z } from 'zod';

import { onboardingStepKeySchema } from './onboarding.js';
import { tenantHealthSchema, tenantSlugSchema, tenantStatusSchema } from './tenants.js';

/**
 * US-152 · Visão 360 e acesso aos módulos do estabelecimento — porta de
 * `GET /v1/platform/tenants/{id}/overview`. Agrega metadados administrativos (tenant, dono, lojas,
 * instalações, checklist de implantação, links) sem nenhum dado operacional do cliente (RN-015) —
 * nunca importar isto de uma tela que precise de pedido/caixa/estoque/financeiro.
 */
export const tenantOverviewTenantSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  slug: tenantSlugSchema,
  status: tenantStatusSchema,
  // US-153 · Ciclo de vida do estabelecimento — controle de concorrência otimista (ADR-020/ADR-023):
  // ecoado sem alteração como `If-Match: "<statusVersion>"` na chamada de transição de status.
  statusVersion: z.number().int().positive(),
  // Máquina de estados computada no servidor (§10 "próxima transição permitida") — a UI nunca
  // decide sozinha quais botões de ação mostrar, só itera este array.
  availableTransitions: z.array(tenantStatusSchema),
  plan: z.string().min(1),
  // Modelo de negócio (business_template.code) — nulo para tenants provisionados antes da US-142.
  template: z.string().nullable(),
  domain: z.string().nullable(),
  createdAt: z.string().datetime({ offset: true }),
  updatedAt: z.string().datetime({ offset: true }),
});

export const tenantOverviewInviteStatusSchema = z.enum(['PENDING', 'ACCEPTED', 'EXPIRED']);

export const tenantOverviewOwnerSchema = z.object({
  name: z.string().min(1),
  email: z.string().email(),
  inviteStatus: tenantOverviewInviteStatusSchema,
});

export const tenantOverviewStoreSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  timezone: z.string().min(1),
});

export const tenantOverviewInstallationStatusSchema = z.enum(['PENDING', 'ACTIVE', 'OFFLINE']);

export const tenantOverviewInstallationSchema = z.object({
  id: z.string().uuid(),
  label: z.string().min(1),
  status: tenantOverviewInstallationStatusSchema,
  health: tenantHealthSchema,
});

export const tenantOverviewDeploymentSchema = z.object({
  completed: z.number().int().nonnegative(),
  total: z.number().int().positive(),
  // Chave (SCREAMING_SNAKE_CASE) do próximo passo pendente do roteiro de implantação — nulo quando
  // completed === total. O front resolve o rótulo em PT-BR via ONBOARDING_STEP_LABELS (onboarding.ts),
  // nunca duplica a tradução aqui.
  nextAction: onboardingStepKeySchema.nullable(),
});

/**
 * §15 "[PENDÊNCIA] Definir quais URLs dos aplicativos existem em cada ambiente antes de habilitar
 * links públicos" — todos os três campos são nulos quando a plataforma não tem o domínio/base
 * configurados para aquele ambiente; o front trata nulo como "Não configurado", nunca como erro.
 */
export const tenantOverviewLinksSchema = z.object({
  publicMenu: z.string().nullable(),
  admin: z.string().nullable(),
  health: z.string().nullable(),
});

export const tenantOverviewResponseSchema = z.object({
  tenant: tenantOverviewTenantSchema,
  owner: tenantOverviewOwnerSchema.nullable(),
  stores: z.array(tenantOverviewStoreSchema),
  installations: z.array(tenantOverviewInstallationSchema),
  deployment: tenantOverviewDeploymentSchema,
  links: tenantOverviewLinksSchema,
});

export type TenantOverviewTenant = z.infer<typeof tenantOverviewTenantSchema>;
export type TenantOverviewInviteStatus = z.infer<typeof tenantOverviewInviteStatusSchema>;
export type TenantOverviewOwner = z.infer<typeof tenantOverviewOwnerSchema>;
export type TenantOverviewStore = z.infer<typeof tenantOverviewStoreSchema>;
export type TenantOverviewInstallationStatus = z.infer<typeof tenantOverviewInstallationStatusSchema>;
export type TenantOverviewInstallation = z.infer<typeof tenantOverviewInstallationSchema>;
export type TenantOverviewDeployment = z.infer<typeof tenantOverviewDeploymentSchema>;
export type TenantOverviewLinks = z.infer<typeof tenantOverviewLinksSchema>;
export type TenantOverviewResponse = z.infer<typeof tenantOverviewResponseSchema>;
