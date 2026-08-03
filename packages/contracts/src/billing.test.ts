import { describe, expect, it } from 'vitest';
import {
  assignBillItemsRequestSchema,
  billResponseSchema,
  partialPaymentResponseSchema,
  registerPartialPaymentRequestSchema,
  waiveServiceFeeRequestSchema,
} from './billing.js';

describe('contrato de divisão de conta (US-027)', () => {
  it('aceita a divisão por pessoa com resíduo de arredondamento (US-027 §4)', () => {
    const bill = billResponseSchema.parse({
      items: [
        {
          id: '0198aabb-2222-7000-8000-000000000001',
          name: 'Pizza Marguerita Média',
          total: '100.00',
          pending: false,
          assignedPerson: null,
        },
      ],
      subtotal: '100.00',
      serviceFee: '0.00',
      total: '100.00',
      splitMode: 'BY_PERSON',
      split: [
        { person: 1, amount: '33.34', serviceFeeAmount: '0.00', serviceFeeWaived: false },
        { person: 2, amount: '33.33', serviceFeeAmount: '0.00', serviceFeeWaived: false },
        { person: 3, amount: '33.33', serviceFeeAmount: '0.00', serviceFeeWaived: false },
      ],
      pendingItems: [],
      hasPendingItems: false,
      amountPaid: null,
      remainingAmount: null,
      unassignedItemIds: [],
    });

    expect(bill.split).toHaveLength(3);
    expect(bill.split.every((part) => typeof part.amount === 'string')).toBe(true);
  });

  it('rejeita valor numérico cru no lugar do total monetário em string (ADR-017)', () => {
    expect(() =>
      billResponseSchema.parse({
        items: [],
        subtotal: 100, // deveria ser "100.00"
        serviceFee: '0.00',
        total: '100.00',
        splitMode: 'BY_PERSON',
        split: [],
        pendingItems: [],
        hasPendingItems: false,
        amountPaid: null,
        remainingAmount: null,
        unassignedItemIds: [],
      }),
    ).toThrow();
  });

  it('marca item pendente (ainda em produção) na lista da divisão (US-027 §4)', () => {
    const bill = billResponseSchema.parse({
      items: [
        {
          id: '0198aabb-2222-7000-8000-000000000002',
          name: 'Pizza Calabresa Grande',
          total: '50.00',
          pending: true,
          assignedPerson: 1,
        },
      ],
      subtotal: '50.00',
      serviceFee: '5.00',
      total: '55.00',
      splitMode: 'BY_ITEM',
      split: [{ person: 1, amount: '55.00', serviceFeeAmount: '5.00', serviceFeeWaived: false }],
      pendingItems: [
        { id: '0198aabb-2222-7000-8000-000000000002', name: 'Pizza Calabresa Grande', status: 'IN_OVEN' },
      ],
      hasPendingItems: true,
      amountPaid: null,
      remainingAmount: null,
      unassignedItemIds: [],
    });

    expect(bill.hasPendingItems).toBe(true);
    expect(bill.pendingItems[0]?.status).toBe('IN_OVEN');
  });

  it('divisão por valor traz o saldo em aberto (US-027 §4, cenário "Divisão por valor")', () => {
    const bill = billResponseSchema.parse({
      items: [],
      subtotal: '180.00',
      serviceFee: '0.00',
      total: '180.00',
      splitMode: 'BY_AMOUNT',
      split: [],
      pendingItems: [],
      hasPendingItems: false,
      amountPaid: '50.00',
      remainingAmount: '130.00',
      unassignedItemIds: [],
    });

    expect(bill.remainingAmount).toBe('130.00');
  });

  it('aceita a atribuição de itens por pessoa (POST .../assign-items)', () => {
    const request = assignBillItemsRequestSchema.parse({
      assignments: [
        { person: 1, itemIds: ['0198aabb-2222-7000-8000-000000000003'] },
        { person: 2, itemIds: ['0198aabb-2222-7000-8000-000000000004'] },
      ],
    });

    expect(request.assignments).toHaveLength(2);
  });

  it('aceita atribuição vazia — a recusa por item não atribuído (RN-017) é decisão do backend, não deste schema', () => {
    const request = assignBillItemsRequestSchema.parse({ assignments: [] });
    expect(request.assignments).toEqual([]);
  });

  it('aceita a retirada de taxa de serviço com o conjunto já isento e o motivo', () => {
    const request = waiveServiceFeeRequestSchema.parse({
      people: 4,
      person: 2,
      alreadyWaivedPersons: [1],
      reason: 'Cliente não concordou com a taxa',
    });

    expect(request.alreadyWaivedPersons).toEqual([1]);
  });

  it('aceita o registro de pagamento parcial e a resposta correspondente', () => {
    const request = registerPartialPaymentRequestSchema.parse({ amount: 50, method: 'CASH' });
    expect(request.amount).toBe(50);

    const response = partialPaymentResponseSchema.parse({
      paymentId: '0198aabb-2222-7000-8000-000000000005',
      amountPaid: '50.00',
      remainingAmount: '130.00',
      total: '180.00',
      sessionStatus: 'BILLREQUESTED',
    });
    expect(response.sessionStatus).toBe('BILLREQUESTED');
  });

  it('recusa forma de pagamento fora do catálogo fechado', () => {
    expect(() => registerPartialPaymentRequestSchema.parse({ amount: 50, method: 'BITCOIN' })).toThrow();
  });
});
