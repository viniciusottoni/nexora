// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PushSubscriptionApi, subscribeToPush, urlBase64ToUint8Array } from './push-notifications.js';

/** jsdom não implementa PushManager/Notification/navigator.serviceWorker — define os três só onde o teste precisa que `isPushSupported()` reporte `true`. */
function stubPushSupport() {
  Object.defineProperty(window, 'PushManager', { value: function PushManager() {}, configurable: true });
  Object.defineProperty(window, 'Notification', { value: function Notification() {}, configurable: true });
  Object.defineProperty(navigator, 'serviceWorker', { value: {}, configurable: true });
}

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };

function jsonResponse(body: unknown, ok = true, status = 200): Response {
  return {
    ok,
    status,
    json: () => Promise.resolve(body),
  } as unknown as Response;
}

function requestUrl(input: RequestInfo | URL) {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

describe('urlBase64ToUint8Array', () => {
  it('converte uma chave VAPID base64url para Uint8Array sem lançar', () => {
    // "AAECAw" (base64url, sem padding) → bytes [0, 1, 2, 3].
    const bytes = urlBase64ToUint8Array('AAECAw');
    expect(Array.from(bytes)).toEqual([0, 1, 2, 3]);
  });
});

describe('PushSubscriptionApi (US-081 §7 — cloud-only)', () => {
  it('envia endpoint + keys para POST /v1/notifications/subscribe com Idempotency-Key', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      expect(requestUrl(input)).toBe('/cloud/v1/notifications/subscribe');
      expect(init?.method).toBe('POST');
      expect(new Headers(init?.headers).get('Idempotency-Key')).toBeTruthy();
      expect(JSON.parse(init?.body as string)).toEqual({
        endpoint: 'https://push.example/abc',
        keys: { p256dh: 'p256dh-key', auth: 'auth-key' },
      });
      return jsonResponse({ subscribed: true });
    });
    const api = new PushSubscriptionApi('/cloud', fetcher);

    const result = await api.subscribe(identity, {
      endpoint: 'https://push.example/abc',
      keys: { p256dh: 'p256dh-key', auth: 'auth-key' },
    });

    expect(result).toEqual({ subscribed: true });
  });

  it('rejeita uma assinatura sem endpoint/keys antes de chamar a rede', async () => {
    const fetcher = vi.fn();
    const api = new PushSubscriptionApi('/cloud', fetcher);

    await expect(api.subscribe(identity, {})).rejects.toThrow('Assinatura de push incompleta.');
    expect(fetcher).not.toHaveBeenCalled();
  });

  it('lança quando o backend recusa a assinatura', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({}, false, 500));
    const api = new PushSubscriptionApi('/cloud', fetcher);

    await expect(
      api.subscribe(identity, { endpoint: 'https://push.example/abc', keys: { p256dh: 'a', auth: 'b' } }),
    ).rejects.toThrow('500');
  });
});

describe('subscribeToPush (US-081 §3 — fluxo completo)', () => {
  afterEach(() => {
    delete (window as { PushManager?: unknown }).PushManager;
    delete (window as { Notification?: unknown }).Notification;
    delete (navigator as { serviceWorker?: unknown }).serviceWorker;
  });

  it('nunca lança quando o navegador não suporta push (sem serviceWorker/PushManager/Notification)', async () => {
    const result = await subscribeToPush({ identity, vapidPublicKey: 'AAECAw' });
    // jsdom (ambiente de teste) não implementa PushManager — isPushSupported() deve reportar false
    // e a função deve devolver `false` em vez de lançar.
    expect(result).toBe(false);
  });

  it('devolve false sem chamar a rede quando não há chave VAPID configurada', async () => {
    const api = { subscribe: vi.fn() } as unknown as PushSubscriptionApi;
    const result = await subscribeToPush({ identity, api });
    expect(result).toBe(false);
    expect(api.subscribe).not.toHaveBeenCalled();
  });

  it('registra o SW, assina o PushManager e envia ao backend quando tudo está disponível', async () => {
    stubPushSupport();
    const toJSON = vi.fn().mockReturnValue({
      endpoint: 'https://push.example/abc',
      keys: { p256dh: 'p256dh-key', auth: 'auth-key' },
    });
    const pushManager = { subscribe: vi.fn().mockResolvedValue({ toJSON }) };
    const registration = { pushManager } as unknown as ServiceWorkerRegistration;
    const api = { subscribe: vi.fn().mockResolvedValue({ subscribed: true }) } as unknown as PushSubscriptionApi;

    const result = await subscribeToPush({
      identity,
      api,
      vapidPublicKey: 'AAECAw',
      registerServiceWorker: vi.fn().mockResolvedValue(registration),
    });

    expect(result).toBe(true);
    expect(pushManager.subscribe).toHaveBeenCalledWith(
      expect.objectContaining({ userVisibleOnly: true }),
    );
    expect(api.subscribe).toHaveBeenCalledWith(identity, {
      endpoint: 'https://push.example/abc',
      keys: { p256dh: 'p256dh-key', auth: 'auth-key' },
    });
  });

  it('devolve false (nunca lança) quando o registro do Service Worker falha', async () => {
    stubPushSupport();
    const api = { subscribe: vi.fn() } as unknown as PushSubscriptionApi;

    const result = await subscribeToPush({
      identity,
      api,
      vapidPublicKey: 'AAECAw',
      registerServiceWorker: vi.fn().mockResolvedValue(undefined),
    });

    expect(result).toBe(false);
    expect(api.subscribe).not.toHaveBeenCalled();
  });
});
