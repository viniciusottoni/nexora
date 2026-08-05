import { describe, expect, it, vi } from 'vitest';
import { DevicePreferencesApi } from './device-preferences-api.js';

const identity = { accessToken: 'token-abc', deviceId: '0198aabb-1111-7000-8000-000000000099', deviceSecret: 'secret-1' };

function jsonResponse(body: unknown, ok = true, status = 200): Response {
  return { ok, status, json: () => Promise.resolve(body) } as unknown as Response;
}

describe('DevicePreferencesApi.updateKdsPreferences (US-042/US-045/US-047)', () => {
  it('envia o patch dentro do envelope {"preferences": {"kds": ...}} — o formato que o backend realmente espera', async () => {
    // Achado durante US-042: `UpdateDevicePreferencesRequest(JsonElement Preferences)`
    // (backend/src/Nexora.Contracts/Devices/UpdateDevicePreferencesRequest.cs) é um record com UMA
    // propriedade "Preferences" — o binder do ASP.NET Core só a preenche se a chave "preferences"
    // existir na raiz do corpo. Mandar `{"kds": {...}}` solto (sem o envelope) deixava
    // `Preferences` como `default(JsonElement)` e o handler quebrava em `GetRawText()`.
    const fetcher = vi.fn().mockResolvedValue(
      jsonResponse({ deviceId: identity.deviceId, preferences: { kds: { stationIds: ['0198aabb-1111-7000-8000-000000000010'] } } }),
    );
    const api = new DevicePreferencesApi('', fetcher);

    await api.updateKdsPreferences(identity, { stationIds: ['0198aabb-1111-7000-8000-000000000010'] });

    expect(fetcher).toHaveBeenCalledOnce();
    const [url, init] = fetcher.mock.calls[0] as [string, RequestInit];
    expect(url).toBe(`/v1/devices/${identity.deviceId}/preferences`);
    expect(init.method).toBe('PATCH');
    expect(JSON.parse(init.body as string)).toEqual({
      preferences: { kds: { stationIds: ['0198aabb-1111-7000-8000-000000000010'] } },
    });
  });

  it('inclui Idempotency-Key por chamada (ADR-020 — obrigatório em PATCH)', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ deviceId: identity.deviceId, preferences: { kds: {} } }));
    const api = new DevicePreferencesApi('', fetcher);

    await api.updateKdsPreferences(identity, { layout: 'GRID' });

    const [, init] = fetcher.mock.calls[0] as [string, RequestInit];
    expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy();
  });
});
