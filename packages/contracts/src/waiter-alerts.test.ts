import { describe, expect, it } from 'vitest';
import { acknowledgeWaiterCallResponseSchema, callWaiterResponseSchema, requestBillRequestSchema, requestBillResponseSchema } from './waiter-alerts.js';

describe('contrato de chamar garçom (US-025)', () => {
  it('aceita a resposta de chamada bem-sucedida', () => {
    const parsed = callWaiterResponseSchema.parse({ acknowledged: true, alreadyPending: false });
    expect(parsed.alreadyPending).toBe(false);
  });

  it('aceita a resposta de chamada repetida', () => {
    const parsed = callWaiterResponseSchema.parse({ acknowledged: true, alreadyPending: true });
    expect(parsed.alreadyPending).toBe(true);
  });

  it('aceita a resposta de confirmação de atendimento', () => {
    const parsed = acknowledgeWaiterCallResponseSchema.parse({ resolved: true, responseSeconds: 42 });
    expect(parsed.responseSeconds).toBe(42);
  });
});

describe('contrato de solicitar a conta (US-026)', () => {
  it('exige people quando splitMode é BY_PERSON', () => {
    expect(() => requestBillRequestSchema.parse({ splitMode: 'BY_PERSON' })).toThrow();
    const parsed = requestBillRequestSchema.parse({ splitMode: 'BY_PERSON', people: 4 });
    expect(parsed.people).toBe(4);
  });

  it('não exige people para SINGLE/BY_ITEM', () => {
    expect(() => requestBillRequestSchema.parse({ splitMode: 'SINGLE' })).not.toThrow();
    expect(() => requestBillRequestSchema.parse({ splitMode: 'BY_ITEM' })).not.toThrow();
  });

  it('aceita a resposta com a sessão em BILLREQUESTED e a preferência registrada', () => {
    const parsed = requestBillResponseSchema.parse({
      session: {
        id: '0198aabb-1111-7000-8000-000000000001',
        tableId: '0198aabb-1111-7000-8000-000000000002',
        tableLabel: '12',
        status: 'BILLREQUESTED',
        openedAt: '2026-08-02T18:00:00.000Z',
        guestCount: 4,
        guestCountConfirmed: true,
        waiterId: null,
        source: 'QR',
        currentItems: [],
        total: '0.00',
        splitMode: 'BY_PERSON',
        splitPeople: 4,
      },
      alreadyRequested: false,
    });
    expect(parsed.session.splitMode).toBe('BY_PERSON');
    expect(parsed.alreadyRequested).toBe(false);
  });
});
