import { describe, expect, it, vi } from 'vitest';
import { CashPanelApi } from './cash-panel-api.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };

function jsonResponse(body: unknown, ok = true, status = 200): Response {
  return {
    ok,
    status,
    json: () => Promise.resolve(body),
  } as unknown as Response;
}

describe('CashPanelApi', () => {
  it('monta a query string de q/sortBy e autentica com Bearer + credenciais de dispositivo', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ sessions: [], summary: { openSessions: 0, totalOpen: '0.00' } }));
    const api = new CashPanelApi('', fetcher);

    await api.listOpenSessions(identity, { search: '12', sortBy: 'table' });

    expect(fetcher).toHaveBeenCalledOnce();
    const [url, init] = fetcher.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/v1/cash/open-sessions?q=12&sortBy=table');
    const headers = new Headers(init.headers);
    expect(headers.get('Authorization')).toBe('Bearer token-abc');
    expect(headers.get('X-Device-Id')).toBe('device-1');
    expect(headers.get('X-Device-Secret')).toBe('secret-1');
  });

  it('não anexa query string quando nenhum filtro é passado', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse({ sessions: [], summary: { openSessions: 0, totalOpen: '0.00' } }));
    const api = new CashPanelApi('', fetcher);

    await api.listOpenSessions(identity);

    const [url] = fetcher.mock.calls[0] as [string];
    expect(url).toBe('/v1/cash/open-sessions');
  });

  it('valida a resposta contra o contrato e devolve sessões + totalizador tipados', async () => {
    const session = {
      sessionId: '0198aabb-1111-7000-8000-000000000001',
      table: '12',
      area: 'Salão',
      openedAt: '2026-08-02T18:00:00.000Z',
      minutesOpen: 47,
      guestCount: 4,
      waiter: { id: '0198aabb-1111-7000-8000-000000000002', name: 'Ana' },
      total: '187.00',
      status: 'BILL_REQUESTED',
      billRequestedAt: '2026-08-02T18:44:00.000Z',
      waitingSeconds: 180,
      pendingItems: 0,
      orderCode: 'A47',
    };
    const fetcher = vi.fn().mockResolvedValue(
      jsonResponse({ sessions: [session], summary: { openSessions: 1, totalOpen: '187.00' } }),
    );
    const api = new CashPanelApi('', fetcher);

    const result = await api.listOpenSessions(identity);

    expect(result.sessions).toEqual([session]);
    expect(result.summary).toEqual({ openSessions: 1, totalOpen: '187.00' });
  });

  it('lança erro com a mensagem do problema RFC 7807 quando a resposta não é ok', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValue(jsonResponse({ detail: 'Não foi possível identificar o estabelecimento.' }, false, 403));
    const api = new CashPanelApi('', fetcher);

    await expect(api.listOpenSessions(identity)).rejects.toThrow('Não foi possível identificar o estabelecimento.');
  });

  it('lança mensagem padrão quando a resposta de erro não é um ProblemDetails válido', async () => {
    const fetcher = vi.fn().mockResolvedValue(jsonResponse(null, false, 500));
    const api = new CashPanelApi('', fetcher);

    await expect(api.listOpenSessions(identity)).rejects.toThrow('Não foi possível carregar o painel do caixa.');
  });
});
