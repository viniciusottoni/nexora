import { authenticatedFetch } from '@nexora/ui';

/**
 * US-155 · Proprietários, usuários iniciais e convites — cliente HTTP autocontido de
 * `GET /v1/platform/tenants/{id}/ownership`, `POST /v1/platform/tenants/{id}/owner-invites`,
 * `DELETE /v1/platform/tenants/{id}/owner-invites/{inviteId}`,
 * `POST /v1/platform/tenants/{id}/ownership-transfers` e
 * `POST /v1/platform/tenants/{id}/ownership/unlock`.
 *
 * Tipos definidos LOCALMENTE (não importados de `@nexora/contracts`) de propósito: o schema espelho
 * já existe em `packages/contracts/src/tenant-ownership.ts`, mas o pacote ainda não o exporta
 * (`index.ts` só ganha `export * from './tenant-ownership.js';` numa integração central posterior —
 * mesma decisão já tomada por `tenant-plan-api.ts`, US-154). Até lá, `tenant-ownership-section.tsx`
 * precisa ser autocontido para não quebrar o build de outras histórias em paralelo.
 */
export type TenantOwnershipOwnerStatus = 'NONE' | 'INVITED' | 'ACTIVE' | 'INACTIVE' | 'BLOCKED';
export type TenantOwnershipInviteStatus = 'PENDING' | 'ACCEPTED' | 'EXPIRED' | 'REVOKED';
export type TenantOwnershipDeliveryStatus = 'PENDING' | 'SENT' | 'FAILED' | 'UNKNOWN';

export interface TenantOwnershipOwner {
  readonly id: string | null;
  readonly name: string | null;
  readonly email: string | null;
  readonly status: TenantOwnershipOwnerStatus;
}

/** Nunca contém segredo (token bruto/hash) — só o servidor sabe, e nem ele reexpõe (ver docstring do backend). */
export interface TenantOwnershipInvite {
  readonly id: string;
  readonly sentTo: string;
  readonly status: TenantOwnershipInviteStatus;
  readonly deliveryStatus: TenantOwnershipDeliveryStatus;
  readonly createdAt: string;
  readonly expiresAt: string;
  readonly consumedAt: string | null;
  readonly revokedAt: string | null;
  readonly revokedReason: string | null;
  readonly reason: string | null;
}

export interface TenantOwnershipTransferHistory {
  readonly id: string;
  readonly previousOwnerUserId: string;
  readonly newOwnerUserId: string;
  readonly reason: string;
  readonly previousKeptAsAdmin: boolean;
  readonly transferredAt: string;
}

export interface TenantOwnershipView {
  readonly owner: TenantOwnershipOwner;
  readonly invites: readonly TenantOwnershipInvite[];
  readonly transfers: readonly TenantOwnershipTransferHistory[];
}

/** Cobre reenvio (e-mail/nome iguais ao atual) e correção (diferentes) — mesmo endpoint/comando no backend. */
export interface CreateOwnerInviteInput {
  readonly name: string;
  readonly email: string;
  readonly reason: string;
}

export interface CreateOwnerInviteResult {
  readonly inviteId: string;
  readonly sentTo: string;
  readonly expiresAt: string;
}

export interface TransferTenantOwnershipInput {
  readonly newOwnerUserId: string;
  readonly reason: string;
  readonly keepPreviousAsAdmin: boolean;
}

export interface TransferTenantOwnershipResult {
  readonly previousOwnerUserId: string;
  readonly newOwnerUserId: string;
  readonly previousKeptAsAdmin: boolean;
  readonly transferredAt: string;
}

export interface UnlockOwnerAccessResult {
  readonly userId: string;
  readonly status: string;
}

export interface ApiProblem extends Error {
  code?: string;
  status?: number;
}

export interface TenantOwnershipApi {
  get(tenantId: string): Promise<TenantOwnershipView>;
  /** `POST` com `Idempotency-Key` por intenção — mesmo padrão de `tenant-plan-api.ts` `update`. */
  createInvite(tenantId: string, input: CreateOwnerInviteInput): Promise<CreateOwnerInviteResult>;
  revokeInvite(tenantId: string, inviteId: string, reason: string): Promise<void>;
  transferOwnership(tenantId: string, input: TransferTenantOwnershipInput): Promise<TransferTenantOwnershipResult>;
  unlock(tenantId: string, reason: string): Promise<UnlockOwnerAccessResult>;
}

export function createTenantOwnershipApi(baseUrl = ''): TenantOwnershipApi {
  let pendingCreateInvite: { intent: string; key: string } | undefined;
  let pendingRevoke: { intent: string; key: string } | undefined;
  let pendingTransfer: { intent: string; key: string } | undefined;
  let pendingUnlock: { intent: string; key: string } | undefined;

  return {
    async get(tenantId) {
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/ownership`,
        { credentials: 'include' },
      );
      if (!response.ok) throw await toApiError(response);
      return (await response.json()) as TenantOwnershipView;
    },

    async createInvite(tenantId, input) {
      const body = JSON.stringify(input);
      const intent = `${tenantId}:${body}`;
      if (!pendingCreateInvite || pendingCreateInvite.intent !== intent) {
        pendingCreateInvite = { intent, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/owner-invites`,
        {
          method: 'POST',
          credentials: 'include',
          headers: { 'content-type': 'application/json', 'idempotency-key': pendingCreateInvite.key },
          body,
        },
      );
      if (!response.ok) throw await toApiError(response);
      const result = (await response.json()) as CreateOwnerInviteResult;
      pendingCreateInvite = undefined;
      return result;
    },

    async revokeInvite(tenantId, inviteId, reason) {
      const body = JSON.stringify({ reason });
      const intent = `${tenantId}:${inviteId}:${body}`;
      if (!pendingRevoke || pendingRevoke.intent !== intent) {
        pendingRevoke = { intent, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/owner-invites/${encodeURIComponent(inviteId)}`,
        {
          method: 'DELETE',
          credentials: 'include',
          headers: { 'content-type': 'application/json', 'idempotency-key': pendingRevoke.key },
          body,
        },
      );
      if (!response.ok) throw await toApiError(response);
      pendingRevoke = undefined;
    },

    async transferOwnership(tenantId, input) {
      const body = JSON.stringify(input);
      const intent = `${tenantId}:${body}`;
      if (!pendingTransfer || pendingTransfer.intent !== intent) {
        pendingTransfer = { intent, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/ownership-transfers`,
        {
          method: 'POST',
          credentials: 'include',
          headers: { 'content-type': 'application/json', 'idempotency-key': pendingTransfer.key },
          body,
        },
      );
      if (!response.ok) throw await toApiError(response);
      const result = (await response.json()) as TransferTenantOwnershipResult;
      pendingTransfer = undefined;
      return result;
    },

    async unlock(tenantId, reason) {
      const body = JSON.stringify({ reason });
      const intent = `${tenantId}:${body}`;
      if (!pendingUnlock || pendingUnlock.intent !== intent) {
        pendingUnlock = { intent, key: crypto.randomUUID() };
      }
      const response = await authenticatedFetch(
        `${baseUrl}/v1/platform/tenants/${encodeURIComponent(tenantId)}/ownership/unlock`,
        {
          method: 'POST',
          credentials: 'include',
          headers: { 'content-type': 'application/json', 'idempotency-key': pendingUnlock.key },
          body,
        },
      );
      if (!response.ok) throw await toApiError(response);
      const result = (await response.json()) as UnlockOwnerAccessResult;
      pendingUnlock = undefined;
      return result;
    },
  };
}

async function toApiError(response: Response): Promise<ApiProblem> {
  const payload = (await response.json().catch(() => undefined)) as
    { detail?: string; code?: string } | undefined;
  const error = new Error(payload?.detail ?? 'Não foi possível concluir a operação.') as ApiProblem;
  if (payload?.code) error.code = payload.code;
  error.status = response.status;
  return error;
}
