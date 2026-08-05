import { describe, expect, it } from 'vitest';
import {
  cashExpectedAmountSchema,
  cashSessionSchema,
  closeCashSessionResponseSchema,
  getCurrentCashSessionResponseSchema,
  listCashMovementsResponseSchema,
  openCashSessionResponseSchema,
  openTableSessionInfoSchema,
  registerCashMovementResponseSchema,
} from './cashier.js';

describe('contrato de caixa (US-055/US-056)', () => {
  it('aceita a sessão aberta com fundo (US-055 §4, cenário "Abertura com fundo")', () => {
    const parsed = openCashSessionResponseSchema.parse({
      session: {
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
      },
    });

    expect(parsed.session.status).toBe('OPEN');
    expect(typeof parsed.session.openingAmount).toBe('string');
  });

  it('aceita a composição do valor esperado (US-055 §4, cenário "Composição do valor esperado")', () => {
    const expected = cashExpectedAmountSchema.parse({
      opening: '200.00',
      cashPayments: '1500.00',
      supplies: '300.00',
      withdrawals: '-150.00',
      total: '1850.00',
    });

    expect(expected.total).toBe('1850.00');
    expect(expected.withdrawals.startsWith('-')).toBe(true);
  });

  it('aceita a resposta de fechamento com divergência (US-055 §4, cenário "Divergência no fechamento")', () => {
    const parsed = closeCashSessionResponseSchema.parse({
      expected: '1850.00',
      counted: '1843.50',
      divergence: '-6.50',
      requiresJustification: true,
      session: {
        id: '0198aabb-2222-7000-8000-000000000001',
        operatorId: '0198aabb-2222-7000-8000-000000000002',
        status: 'CLOSED',
        openingAmount: '200.00',
        openedAt: '2026-08-05T10:00:00Z',
        closedAt: '2026-08-05T22:00:00Z',
        expectedAmount: '1850.00',
        countedAmount: '1843.50',
        divergence: '-6.50',
        justification: 'Troco entregue a mais',
      },
    });

    expect(parsed.requiresJustification).toBe(true);
    expect(parsed.session.justification).toBe('Troco entregue a mais');
  });

  it('aceita a lista de mesas abertas do 422 OPEN_TABLES (US-055 §7)', () => {
    const table = openTableSessionInfoSchema.parse({ table: '12', total: '87.00' });
    expect(table.table).toBe('12');
  });

  it('aceita o registro de sangria (US-056 §4, cenário "Sangria registrada")', () => {
    const parsed = registerCashMovementResponseSchema.parse({
      movement: {
        id: '0198aabb-2222-7000-8000-000000000003',
        type: 'WITHDRAWAL',
        amount: '500.00',
        reason: 'Sangria de segurança',
        occurredAt: '2026-08-05T12:00:00Z',
        createdBy: '0198aabb-2222-7000-8000-000000000002',
        authorizedBy: null,
      },
      newExpected: '1000.00',
    });

    expect(parsed.movement.type).toBe('WITHDRAWAL');
    expect(parsed.newExpected).toBe('1000.00');
  });

  it('aceita o histórico de movimentos do turno (US-056 §10)', () => {
    const parsed = listCashMovementsResponseSchema.parse({
      movements: [
        {
          id: '0198aabb-2222-7000-8000-000000000003',
          type: 'SUPPLY',
          amount: '200.00',
          reason: 'Troco inicial',
          occurredAt: '2026-08-05T12:00:00Z',
          createdBy: '0198aabb-2222-7000-8000-000000000002',
          authorizedBy: null,
        },
      ],
    });

    expect(parsed.movements).toHaveLength(1);
  });

  it('aceita a sessão corrente com composição do valor esperado (GET /v1/cash-sessions/current)', () => {
    const parsed = getCurrentCashSessionResponseSchema.parse({
      session: cashSessionSchema.parse({
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
      }),
      expected: {
        opening: '200.00',
        cashPayments: '0.00',
        supplies: '0.00',
        withdrawals: '0.00',
        total: '200.00',
      },
    });

    expect(parsed.expected.total).toBe('200.00');
  });
});
