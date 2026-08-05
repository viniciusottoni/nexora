import { describe, expect, it, vi } from 'vitest';
import { CashSessionApi, CashSessionApiError } from './cash-session-api.js';

const identity = {
  accessToken: 'access-local',
  deviceId: '0198aabb-1111-7000-8000-000000000001',
  deviceSecret: 'segredo-local',
};

const session = {
  id: '0198aabb-2222-7000-8000-000000000001',
  operatorId: '0198aabb-2222-7000-8000-000000000002',
  status: 'OPEN',
  openingAmount: '200.00',
  openedAt: '2026-08-05T10:00:00Z',
  closedAt: null,
  expectedAmount: null,
  countedAmount: null,
  divergence: null,
  justification: null,
};

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

describe('CashSessionApi', () => {
  it('abre o caixa com Idempotency-Key e devolve a sessão OPEN (US-055 §4)', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ session }));
    const api = new CashSessionApi('', fetcher);

    const response = await api.open(identity, { openingAmount: 200 });

    expect(response.session.status).toBe('OPEN');
    const [url, init] = fetcher.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toContain('/v1/cash-sessions/open');
    expect(init.method).toBe('POST');
    expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy();
    expect(JSON.parse(init.body as string)).toMatchObject({ openingAmount: 200 });
  });

  it('propaga CASH_SESSION_ALREADY_OPEN com meta.sessionId (US-055 §4, "Um caixa por operador e turno")', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse(
        { detail: 'Já existe um caixa aberto.', code: 'CASH_SESSION_ALREADY_OPEN', meta: { sessionId: 'existing-id' } },
        409,
      ),
    );
    const api = new CashSessionApi('', fetcher);

    await expect(api.open(identity, { openingAmount: 100 })).rejects.toMatchObject({
      code: 'CASH_SESSION_ALREADY_OPEN',
      meta: { sessionId: 'existing-id' },
    });
  });

  it('busca a sessão corrente com a composição do valor esperado (US-055 §4, "Composição do valor esperado")', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        session,
        expected: { opening: '200.00', cashPayments: '1500.00', supplies: '300.00', withdrawals: '-150.00', total: '1850.00' },
      }),
    );
    const api = new CashSessionApi('', fetcher);

    const response = await api.getCurrent(identity);

    expect(response.expected.total).toBe('1850.00');
    const [url] = fetcher.mock.calls[0] as unknown as [string];
    expect(url).toContain('/v1/cash-sessions/current');
  });

  it('fecha o caixa e devolve a divergência (US-055 §4, "Divergência no fechamento")', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        expected: '1850.00',
        counted: '1843.50',
        divergence: '-6.50',
        requiresJustification: true,
        session: { ...session, status: 'CLOSED' },
      }),
    );
    const api = new CashSessionApi('', fetcher);

    const response = await api.close(identity, session.id, { countedAmount: 1843.5, justification: 'Troco a mais' });

    expect(response.requiresJustification).toBe(true);
    const [url, init] = fetcher.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toContain(`/v1/cash-sessions/${session.id}/close`);
    expect(JSON.parse(init.body as string)).toMatchObject({ countedAmount: 1843.5, justification: 'Troco a mais' });
  });

  it('envia X-Authorization-Token no fechamento quando informado (RN-018, mesa aberta)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        expected: '200.00',
        counted: '200.00',
        divergence: '0.00',
        requiresJustification: false,
        session: { ...session, status: 'CLOSED' },
      }),
    );
    const api = new CashSessionApi('', fetcher);

    await api.close(identity, session.id, { countedAmount: 200 }, 'token-mesa-aberta');

    const [, init] = fetcher.mock.calls[0] as unknown as [string, RequestInit];
    expect(new Headers(init.headers).get('X-Authorization-Token')).toBe('token-mesa-aberta');
  });

  it('propaga OPEN_TABLES com meta.openSessions (US-055 §7, cenário "Mesa aberta no fechamento")', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse(
        {
          detail: 'Existem mesas ainda abertas.',
          code: 'OPEN_TABLES',
          meta: { openSessions: [{ table: '12', total: '87.00' }] },
        },
        422,
      ),
    );
    const api = new CashSessionApi('', fetcher);

    await expect(api.close(identity, session.id, { countedAmount: 200 })).rejects.toMatchObject({
      code: 'OPEN_TABLES',
      meta: { openSessions: [{ table: '12', total: '87.00' }] },
    });
  });

  it('registra sangria e devolve o novo valor esperado (US-056 §4, "Sangria registrada")', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        movement: {
          id: '0198aabb-3333-7000-8000-000000000001',
          type: 'WITHDRAWAL',
          amount: '500.00',
          reason: 'Sangria de segurança',
          occurredAt: '2026-08-05T12:00:00Z',
          createdBy: identity.deviceId,
          authorizedBy: null,
        },
        newExpected: '1000.00',
      }),
    );
    const api = new CashSessionApi('', fetcher);

    const response = await api.registerMovement(identity, { type: 'WITHDRAWAL', amount: 500, reason: 'Sangria de segurança' });

    expect(response.newExpected).toBe('1000.00');
    const [url, init] = fetcher.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toContain('/v1/cash-sessions/movements');
    expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy();
  });

  it('propaga AUTHORIZATION_REQUIRED quando a sangria excede o limite (US-056 §4, "Sangria acima do limite")', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({ detail: 'Autorização necessária.', code: 'AUTHORIZATION_REQUIRED' }, 403),
    );
    const api = new CashSessionApi('', fetcher);

    await expect(
      api.registerMovement(identity, { type: 'WITHDRAWAL', amount: 800, reason: 'Depósito' }),
    ).rejects.toMatchObject({ code: 'AUTHORIZATION_REQUIRED' });
  });

  it('lista o histórico de movimentos do turno (US-056 §10)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        movements: [
          {
            id: '0198aabb-3333-7000-8000-000000000001',
            type: 'SUPPLY',
            amount: '200.00',
            reason: 'Troco inicial',
            occurredAt: '2026-08-05T12:00:00Z',
            createdBy: identity.deviceId,
            authorizedBy: null,
          },
        ],
      }),
    );
    const api = new CashSessionApi('', fetcher);

    const response = await api.listMovements(identity);

    expect(response.movements).toHaveLength(1);
  });

  it('propaga NO_OPEN_CASH_SESSION (US-056 §4, "Movimento sem caixa aberto")', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ detail: 'Não há caixa aberto.', code: 'NO_OPEN_CASH_SESSION' }, 409));
    const api = new CashSessionApi('', fetcher);

    await expect(api.listMovements(identity)).rejects.toMatchObject({ code: 'NO_OPEN_CASH_SESSION' });
  });

  it('autoriza uma ação sensível via /v1/auth/authorize (ADR-023)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({
        authorizationToken: 'token-autorizacao',
        expiresIn: 120,
        authorizedBy: { id: '0198aabb-7777-7000-8000-000000000001', name: 'Gerente Ana' },
      }),
    );
    const api = new CashSessionApi('', fetcher);

    const grant = await api.authorize(identity, { action: 'WITHDRAWAL_ABOVE_LIMIT', pin: '4433' });

    expect(grant.authorizationToken).toBe('token-autorizacao');
    const [url, init] = fetcher.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toContain('/v1/auth/authorize');
    expect(JSON.parse(init.body as string)).toMatchObject({ action: 'WITHDRAWAL_ABOVE_LIMIT', pin: '4433' });
  });

  it('erro genérico vira CashSessionApiError com mensagem amigável', async () => {
    const fetcher = vi.fn(async () => jsonResponse({}, 500));
    const api = new CashSessionApi('', fetcher);

    await expect(api.getCurrent(identity)).rejects.toBeInstanceOf(CashSessionApiError);
  });
});
