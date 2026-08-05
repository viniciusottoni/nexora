import { describe, expect, it, vi } from 'vitest';
import { KdsStationsApi, StationsApiError, stationColorCssValue } from './stations-api.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };

function jsonResponse(body: unknown, ok = true, status = 200): Response {
  return { ok, status, json: () => Promise.resolve(body) } as unknown as Response;
}

describe('KdsStationsApi (US-042)', () => {
  it('lista as praças em GET /v1/catalog/stations autenticado com credenciais de dispositivo', async () => {
    const station = {
      id: '0198aabb-1111-7000-8000-000000000010',
      code: 'FORNO',
      name: 'Forno',
      color: 'red',
      capacitySlots: null,
      isBottleneck: true,
      position: 0,
      isActive: true,
      linkedProductCount: 4,
    };
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ items: [station] }));
    const api = new KdsStationsApi('', fetcher);

    const result = await api.list(identity);

    expect(result.items).toEqual([station]);
    const [url, init] = fetcher.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/v1/catalog/stations');
    const headers = new Headers(init.headers);
    expect(headers.get('Authorization')).toBe('Bearer token-abc');
    expect(headers.get('X-Device-Id')).toBe('device-1');
  });

  it('propaga o code estável do RFC 7807 quando a listagem falha', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ detail: 'Sem permissão', code: 'AUTH_FORBIDDEN' }, false, 403));
    const api = new KdsStationsApi('', fetcher);

    await expect(api.list(identity)).rejects.toMatchObject({ code: 'AUTH_FORBIDDEN' });
    await expect(api.list(identity)).rejects.toBeInstanceOf(StationsApiError);
  });
});

describe('stationColorCssValue', () => {
  it('mapeia a chave semântica para o token CSS da rampa de marca (nunca hex literal — ADR-010)', () => {
    expect(stationColorCssValue('red')).toBe('var(--nx-danger-500)');
    expect(stationColorCssValue('teal')).toBe('var(--nx-teal-500)');
  });

  it('cai num valor neutro para cor desconhecida ou ausente', () => {
    expect(stationColorCssValue(null)).toBe('var(--text-inverse, #fff)');
    expect(stationColorCssValue('cor-inexistente')).toBe('var(--text-inverse, #fff)');
  });
});
