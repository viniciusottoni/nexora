import { describe, expect, it } from 'vitest';
import {
  openTableSessionRequestSchema,
  publicTableAccessResponseSchema,
  tableSessionSchema,
  updateTableSessionRequestSchema,
} from './operation-table-sessions.js';

const sessionId = '0198aabb-3333-7000-8000-000000000003';
const tableId = '0198aabb-2222-7000-8000-000000000002';

function buildSession(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: sessionId,
    tableId,
    tableLabel: '12',
    status: 'OPEN',
    openedAt: '2026-08-02T20:12:04.221Z',
    guestCount: 4,
    guestCountConfirmed: true,
    waiterId: null,
    source: 'WAITER',
    currentItems: [],
    total: '0.00',
    ...overrides,
  };
}

describe('contratos de sessao de mesa (US-021/US-022)', () => {
  it('valida a abertura pelo garcom', () => {
    expect(openTableSessionRequestSchema.parse({ guestCount: 4 })).toEqual({ guestCount: 4 });
  });

  it('recusa abertura com zero pessoas', () => {
    expect(() => openTableSessionRequestSchema.parse({ guestCount: 0 })).toThrow();
  });

  it('recusa PATCH sem nenhum campo', () => {
    expect(() => updateTableSessionRequestSchema.parse({})).toThrow(
      'Informe ao menos a nova contagem de pessoas ou o novo garçom responsável',
    );
  });

  it('aceita PATCH so com guestCount ou so com waiterId', () => {
    expect(updateTableSessionRequestSchema.parse({ guestCount: 5 })).toEqual({ guestCount: 5 });
    expect(updateTableSessionRequestSchema.parse({ waiterId: sessionId })).toEqual({ waiterId: sessionId });
  });

  it('representa uma sessao aberta com contagem confirmada (origem WAITER)', () => {
    const session = tableSessionSchema.parse(buildSession());
    expect(session.source).toBe('WAITER');
    expect(session.guestCountConfirmed).toBe(true);
    expect(session.currentItems).toEqual([]);
  });

  it('representa uma sessao aberta por QR com contagem pendente de confirmacao', () => {
    const session = tableSessionSchema.parse(
      buildSession({ source: 'QR', guestCount: 1, guestCountConfirmed: false, waiterId: null }),
    );
    expect(session.source).toBe('QR');
    expect(session.guestCountConfirmed).toBe(false);
  });

  it('valida a resposta de acesso publico pelo QR Code com sessionToken', () => {
    const response = publicTableAccessResponseSchema.parse({
      table: { id: tableId, label: '12', areaName: 'Salão' },
      session: buildSession(),
      sessionToken: 'token-anonimo-de-teste',
    });

    expect(response.sessionToken).toBe('token-anonimo-de-teste');
    expect(response.table.label).toBe('12');
  });
});
