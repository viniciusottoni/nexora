import { z } from 'zod';

const idSchema = z.string().uuid();

/**
 * US-155 · Proprietários, usuários iniciais e convites — espelha
 * `Nexora.Contracts.Tenants.TenantOwnershipContracts` (backend). Ainda não exportado por
 * `index.ts` (integração central posterior, ver relatório final da tarefa) — importado direto de
 * `./tenant-ownership.js` enquanto isso, mesma convenção de `tenant-plan.ts` (US-154).
 */
export const tenantOwnershipOwnerStatusSchema = z.enum(['NONE', 'INVITED', 'ACTIVE', 'INACTIVE', 'BLOCKED']);
export type TenantOwnershipOwnerStatus = z.infer<typeof tenantOwnershipOwnerStatusSchema>;

export const tenantOwnershipInviteStatusSchema = z.enum(['PENDING', 'ACCEPTED', 'EXPIRED', 'REVOKED']);
export type TenantOwnershipInviteStatus = z.infer<typeof tenantOwnershipInviteStatusSchema>;

export const tenantOwnershipDeliveryStatusSchema = z.enum(['PENDING', 'SENT', 'FAILED', 'UNKNOWN']);
export type TenantOwnershipDeliveryStatus = z.infer<typeof tenantOwnershipDeliveryStatusSchema>;

export const tenantOwnershipOwnerSchema = z.object({
  id: idSchema.nullable(),
  name: z.string().nullable(),
  email: z.string().nullable(),
  status: tenantOwnershipOwnerStatusSchema,
});

/** Nunca contém `secretHash`/`token` — ver `OwnershipContractsSecretLeakTests` (backend, Nexora.UnitTests). */
export const tenantOwnershipInviteSchema = z.object({
  id: idSchema,
  sentTo: z.string(),
  status: tenantOwnershipInviteStatusSchema,
  deliveryStatus: tenantOwnershipDeliveryStatusSchema,
  createdAt: z.string().datetime({ offset: true }),
  expiresAt: z.string().datetime({ offset: true }),
  consumedAt: z.string().datetime({ offset: true }).nullable(),
  revokedAt: z.string().datetime({ offset: true }).nullable(),
  revokedReason: z.string().nullable(),
  reason: z.string().nullable(),
});

export const tenantOwnershipTransferHistorySchema = z.object({
  id: idSchema,
  previousOwnerUserId: idSchema,
  newOwnerUserId: idSchema,
  reason: z.string(),
  previousKeptAsAdmin: z.boolean(),
  transferredAt: z.string().datetime({ offset: true }),
});

export const tenantOwnershipResponseSchema = z.object({
  owner: tenantOwnershipOwnerSchema,
  invites: z.array(tenantOwnershipInviteSchema),
  transfers: z.array(tenantOwnershipTransferHistorySchema),
});

/** Corpo de `POST /v1/platform/tenants/{id}/owner-invites` — cobre reenvio (e-mail igual) e correção (e-mail diferente). */
export const createOwnerInviteRequestSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome do proprietário.'),
  email: z.string().trim().email('Informe um e-mail válido.'),
  reason: z.string().trim().min(1, 'O motivo é obrigatório.'),
});

export const createOwnerInviteResponseSchema = z.object({
  inviteId: idSchema,
  sentTo: z.string(),
  expiresAt: z.string().datetime({ offset: true }),
});

/** Corpo de `DELETE /v1/platform/tenants/{id}/owner-invites/{inviteId}`. */
export const revokeOwnerInviteRequestSchema = z.object({
  reason: z.string().trim().min(1, 'O motivo é obrigatório.'),
});

/** Corpo de `POST /v1/platform/tenants/{id}/ownership-transfers`. */
export const transferTenantOwnershipRequestSchema = z.object({
  newOwnerUserId: idSchema,
  reason: z.string().trim().min(1, 'O motivo é obrigatório.'),
  keepPreviousAsAdmin: z.boolean(),
});

export const transferTenantOwnershipResponseSchema = z.object({
  previousOwnerUserId: idSchema,
  newOwnerUserId: idSchema,
  previousKeptAsAdmin: z.boolean(),
  transferredAt: z.string().datetime({ offset: true }),
});

/** Corpo de `POST /v1/platform/tenants/{id}/ownership/unlock`. */
export const unlockOwnerAccessRequestSchema = z.object({
  reason: z.string().trim().min(1, 'O motivo é obrigatório.'),
});

export const unlockOwnerAccessResponseSchema = z.object({
  userId: idSchema,
  status: z.string(),
});

export type TenantOwnershipOwner = z.infer<typeof tenantOwnershipOwnerSchema>;
export type TenantOwnershipInvite = z.infer<typeof tenantOwnershipInviteSchema>;
export type TenantOwnershipTransferHistory = z.infer<typeof tenantOwnershipTransferHistorySchema>;
export type TenantOwnershipResponse = z.infer<typeof tenantOwnershipResponseSchema>;
export type CreateOwnerInviteRequest = z.infer<typeof createOwnerInviteRequestSchema>;
export type CreateOwnerInviteResponse = z.infer<typeof createOwnerInviteResponseSchema>;
export type RevokeOwnerInviteRequest = z.infer<typeof revokeOwnerInviteRequestSchema>;
export type TransferTenantOwnershipRequest = z.infer<typeof transferTenantOwnershipRequestSchema>;
export type TransferTenantOwnershipResponse = z.infer<typeof transferTenantOwnershipResponseSchema>;
export type UnlockOwnerAccessRequest = z.infer<typeof unlockOwnerAccessRequestSchema>;
export type UnlockOwnerAccessResponse = z.infer<typeof unlockOwnerAccessResponseSchema>;
