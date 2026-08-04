import {
  subscribePushRequestSchema,
  subscribePushResponseSchema,
  type SubscribePushResponse,
} from '@nexora/contracts';
import { operationalAuthenticatedFetch, type OperationalRequestIdentity } from '@nexora/ui';

// Ver comentário equivalente em notification-center-api.ts/table-map-api.ts.
const browserFetch: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args);

const IS_LOCAL_DEV =
  globalThis.location?.hostname === 'localhost' || globalThis.location?.hostname === '127.0.0.1';

/**
 * US-081 §3/§7 — `POST /v1/notifications/subscribe` existe SÓ no host cloud (`Nexora.Api.Cloud`),
 * nunca no edge (doc. 02 §7.2: "o push é enviado pela nuvem, não pelo edge"). web-pos, diferente de
 * web-admin (que já distingue `CLOUD_API_BASE_URL`/`EDGE_API_BASE_URL` via `IS_LOCAL_DEV` — ver
 * `apps/web-admin/src/app.tsx`), é servido INTEIRAMENTE pelo edge server da loja em produção (mesma
 * origem de `/v1/local/branding`, `/hubs/*` — ver `vite.config.ts`, que só proxia `/v1` e `/hubs`
 * para o edge): não existe hoje nenhuma rota embutida até a nuvem.
 *
 * [DECISÃO] Reaproveita o MESMO padrão `IS_LOCAL_DEV` de web-admin (fallback `/cloud` em dev local,
 * proxiado no `vite.config.ts` deste app para a API cloud local) e, fora do dev local, resolve a
 * origem por uma variável de build (`VITE_CLOUD_API_BASE_URL`, URL absoluta configurada por
 * instalação — nunca fixa no código, ADR-013). Sem a variável configurada em produção, a
 * assinatura de push simplesmente não é enviada (`subscribeToPush` abaixo nunca lança) — o pior caso
 * é o gestor fora da loja não receber push, limitação já documentada em US-081 §9 para quando a
 * nuvem está inacessível; a central in-app (WebSocket local) continua funcionando normalmente.
 */
export const CLOUD_NOTIFICATIONS_BASE_URL: string = IS_LOCAL_DEV
  ? '/cloud'
  : (import.meta.env.VITE_CLOUD_API_BASE_URL ?? '');

/** Cliente HTTP de `POST /v1/notifications/subscribe` (cloud) — mesmo padrão de `NotificationCenterApi`. */
export class PushSubscriptionApi {
  constructor(
    private readonly baseUrl: string = CLOUD_NOTIFICATIONS_BASE_URL,
    private readonly fetcher: typeof fetch = browserFetch,
  ) {}

  async subscribe(
    identity: Readonly<OperationalRequestIdentity>,
    subscription: PushSubscriptionJSON,
  ): Promise<SubscribePushResponse> {
    if (!subscription.endpoint || !subscription.keys?.p256dh || !subscription.keys?.auth) {
      throw new Error('Assinatura de push incompleta.');
    }
    const body = subscribePushRequestSchema.parse({
      endpoint: subscription.endpoint,
      keys: { p256dh: subscription.keys.p256dh, auth: subscription.keys.auth },
    });
    const response = await operationalAuthenticatedFetch(
      `${this.baseUrl}/v1/notifications/subscribe`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(body),
      },
      identity,
      this.fetcher,
    );
    if (!response.ok) {
      throw new Error(`POST /v1/notifications/subscribe recusado com status ${response.status}`);
    }
    return subscribePushResponseSchema.parse(await response.json());
  }
}

/** Chave pública VAPID (RFC 8292) — espelha `WebPush:PublicKeyBase64Url` do backend (`Nexora.Infrastructure/Notifications/WebPushOptions.cs`). */
export function readVapidPublicKey(): string | undefined {
  return import.meta.env.VITE_WEB_PUSH_PUBLIC_KEY || undefined;
}

/** Converte a chave pública VAPID (base64url) para o `Uint8Array` que `PushManager.subscribe` exige. */
export function urlBase64ToUint8Array(base64Url: string): Uint8Array<ArrayBuffer> {
  const padding = '='.repeat((4 - (base64Url.length % 4)) % 4);
  const base64 = (base64Url + padding).replace(/-/g, '+').replace(/_/g, '/');
  const raw = atob(base64);
  // `Uint8Array<ArrayBuffer>` explícito (não o `Uint8Array<ArrayBufferLike>` que a assinatura
  // padrão do construtor infere) — é o que `PushManager.subscribe` exige em `applicationServerKey`
  // (`BufferSource` não aceita `ArrayBufferLike`, que incluiria `SharedArrayBuffer`).
  const bytes = new Uint8Array(new ArrayBuffer(raw.length));
  for (let index = 0; index < raw.length; index += 1) bytes[index] = raw.charCodeAt(index);
  return bytes;
}

export function isPushSupported(): boolean {
  return (
    typeof window !== 'undefined' &&
    typeof navigator !== 'undefined' &&
    'serviceWorker' in navigator &&
    'PushManager' in window &&
    'Notification' in window
  );
}

/**
 * Registra o Service Worker DEDICADO a push (`/push-sw.js`) — separado do de branding
 * (`register-branding-worker.ts`/`branding-sw.js`, que só trata cache de `/v1/public/branding`);
 * cada um cuida de um evento de navegador diferente (`fetch` vs `push`/`notificationclick`) e
 * misturá-los complicaria o cache-busting de cada um.
 */
export async function registerPushServiceWorker(): Promise<ServiceWorkerRegistration | undefined> {
  if (!isPushSupported()) return undefined;
  try {
    return await navigator.serviceWorker.register('/push-sw.js');
  } catch {
    return undefined;
  }
}

export interface SubscribeToPushOptions {
  readonly identity: Readonly<OperationalRequestIdentity>;
  readonly api?: PushSubscriptionApi;
  readonly vapidPublicKey?: string;
  readonly registerServiceWorker?: () => Promise<ServiceWorkerRegistration | undefined>;
}

/**
 * Fluxo completo (US-081 §3): registra o SW + `PushManager.subscribe` + envia a assinatura ao
 * backend. Assume que a PERMISSÃO já foi concedida por quem chama (`Notification.permission ===
 * 'granted'`, decidido em `useNotificationCenter`) — esta função só cuida do resto. Nunca lança:
 * falha aqui não pode derrubar a central de notificações in-app, que já funciona sem push (US-081
 * §4, cenário "Permissão não concedida").
 */
export async function subscribeToPush(options: SubscribeToPushOptions): Promise<boolean> {
  const vapidPublicKey = options.vapidPublicKey ?? readVapidPublicKey();
  if (!isPushSupported() || !vapidPublicKey) return false;

  try {
    const registration = await (options.registerServiceWorker ?? registerPushServiceWorker)();
    if (!registration) return false;
    const subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(vapidPublicKey),
    });
    const api = options.api ?? new PushSubscriptionApi();
    await api.subscribe(options.identity, subscription.toJSON());
    return true;
  } catch {
    return false;
  }
}
