import { describe, expect, it } from 'vitest';
import { tableMapEntrySchema, tableMapResponseSchema } from './tables.js';

describe('contrato do mapa de mesas (US-023)', () => {
  it('aceita mesa livre sem sessão', () => {
    const entry = tableMapEntrySchema.parse({
      id: '0198aabb-1111-7000-8000-000000000001',
      label: '12',
      area: 'Salão',
      status: 'FREE',
      seats: 4,
      session: null,
      flags: { waiterCalled: false, billRequested: false, itemsReadyToServe: 0, aboveAvgDuration: false },
    });
    expect(entry.session).toBeNull();
  });

  it('aceita mesa ocupada com valor consumido representado como string (ADR-017)', () => {
    const entry = tableMapEntrySchema.parse({
      id: '0198aabb-1111-7000-8000-000000000002',
      label: '7',
      area: 'Varanda',
      status: 'BILL_REQUESTED',
      seats: 6,
      session: {
        openedAt: '2026-08-02T18:00:00.000Z',
        minutesOpen: 47,
        total: '187.00',
        guestCount: 4,
        waiter: { id: '0198aabb-1111-7000-8000-000000000003', name: 'Ana' },
        sessionId: '0198aabb-1111-7000-8000-000000000005',
      },
      flags: { waiterCalled: false, billRequested: true, itemsReadyToServe: 2, aboveAvgDuration: true },
    });
    expect(typeof entry.session?.total).toBe('string');
    expect(entry.flags.itemsReadyToServe).toBe(2);
  });

  it('rejeita valor numérico cru no lugar do total monetário em string', () => {
    expect(() =>
      tableMapEntrySchema.parse({
        id: '0198aabb-1111-7000-8000-000000000004',
        label: '3',
        area: 'Salão',
        status: 'OCCUPIED',
        seats: 2,
        session: {
          openedAt: '2026-08-02T18:00:00.000Z',
          minutesOpen: 5,
          total: 187, // deveria ser "187.00"
          guestCount: 1,
          waiter: null,
          sessionId: '0198aabb-1111-7000-8000-000000000006',
        },
        flags: { waiterCalled: false, billRequested: false, itemsReadyToServe: 0, aboveAvgDuration: false },
      }),
    ).toThrow();
  });

  it('envelopa a lista em { tables }', () => {
    const response = tableMapResponseSchema.parse({ tables: [] });
    expect(response.tables).toEqual([]);
  });
});
