import { describe, expect, it, vi } from 'vitest';
import { operationalAuthenticatedFetch } from './operational-authenticated-fetch.js';

describe('operationalAuthenticatedFetch', () => {
  it('vincula bearer ao id e segredo do dispositivo', async () => {
    let received: RequestInit | undefined;
    const fetcher: typeof fetch = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      received = init;
      return new Response(null, { status: 204 });
    });

    await operationalAuthenticatedFetch(
      '/v1/devices',
      { headers: { Accept: 'application/json' } },
      {
        accessToken: 'access-local',
        deviceId: '0198aabb-1111-7000-8000-000000000001',
        deviceSecret: 'segredo-local',
      },
      fetcher,
    );

    const headers = new Headers(received?.headers);
    expect(headers.get('Authorization')).toBe('Bearer access-local');
    expect(headers.get('X-Device-Id')).toBe('0198aabb-1111-7000-8000-000000000001');
    expect(headers.get('X-Device-Secret')).toBe('segredo-local');
    expect(headers.get('Accept')).toBe('application/json');
  });
});
