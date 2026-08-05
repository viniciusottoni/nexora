import { describe, expect, it } from 'vitest';
import { openSessionEntrySchema, openSessionsResponseSchema } from './cashier-open-sessions.js';

describe('contrato do painel do caixa (US-050)', () => {
  it('aceita uma sessão com conta solicitada e tempo de espera', () => {
    const entry = openSessionEntrySchema.parse({
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
    });
    expect(entry.status).toBe('BILL_REQUESTED');
    expect(typeof entry.total).toBe('string');
    expect(entry.waitingSeconds).toBe(180);
  });

  it('aceita uma sessão aberta sem conta solicitada (waitingSeconds/billRequestedAt/orderCode nulos)', () => {
    const entry = openSessionEntrySchema.parse({
      sessionId: '0198aabb-1111-7000-8000-000000000003',
      table: '5',
      area: 'Varanda',
      openedAt: '2026-08-02T18:00:00.000Z',
      minutesOpen: 10,
      guestCount: 2,
      waiter: null,
      total: '0.00',
      status: 'OPEN',
      billRequestedAt: null,
      waitingSeconds: null,
      pendingItems: 2,
      orderCode: null,
    });
    expect(entry.waiter).toBeNull();
    expect(entry.waitingSeconds).toBeNull();
  });

  it('rejeita valor numérico cru no lugar do total monetário em string (ADR-017)', () => {
    expect(() =>
      openSessionEntrySchema.parse({
        sessionId: '0198aabb-1111-7000-8000-000000000004',
        table: '3',
        area: 'Salão',
        openedAt: '2026-08-02T18:00:00.000Z',
        minutesOpen: 5,
        guestCount: 1,
        waiter: null,
        total: 187, // deveria ser "187.00"
        status: 'OPEN',
        billRequestedAt: null,
        waitingSeconds: null,
        pendingItems: 0,
        orderCode: null,
      }),
    ).toThrow();
  });

  it('envelopa a lista em { sessions, summary }', () => {
    const response = openSessionsResponseSchema.parse({
      sessions: [],
      summary: { openSessions: 0, totalOpen: '0.00' },
    });
    expect(response.sessions).toEqual([]);
    expect(response.summary.totalOpen).toBe('0.00');
  });
});
