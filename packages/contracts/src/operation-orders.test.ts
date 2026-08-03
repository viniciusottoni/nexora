import { describe, expect, it } from 'vitest';
import {
  addOrderItemRequestSchema,
  orderItemResponseSchema,
  repeatOrderItemResponseSchema,
  sessionConsumptionResponseSchema,
  tableConsumptionEventSchema,
} from './operation-orders.js';

const orderId = '0198aabb-4444-7000-8000-000000000004';
const itemId = '0198aabb-5555-7000-8000-000000000005';
const variantId = '0198aabb-6666-7000-8000-000000000006';

function buildItem(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: itemId,
    orderId,
    variantId,
    name: 'Pizza G Mussarela',
    quantity: 1,
    unitPrice: '52.00',
    totalPrice: '52.00',
    status: 'QUEUED',
    notes: null,
    stationId: null,
    repeatedFromItemId: null,
    modifiers: [],
    fractions: [],
    ...overrides,
  };
}

describe('contratos de consumo/repeticao de item (US-024/US-028)', () => {
  it('valida o lancamento minimo de item', () => {
    const request = addOrderItemRequestSchema.parse({ variantId, quantity: 2 });
    expect(request.quantity).toBe(2);
  });

  it('recusa quantidade zero', () => {
    expect(() => addOrderItemRequestSchema.parse({ variantId, quantity: 0 })).toThrow();
  });

  it('aceita modificadores e fracoes opcionais', () => {
    const request = addOrderItemRequestSchema.parse({
      variantId,
      quantity: 1,
      notes: 'Sem cebola',
      modifiers: [{ modifierId: variantId, quantity: 1 }],
      fractions: [{ variantId, weight: 0.5 }],
    });
    expect(request.modifiers).toHaveLength(1);
    expect(request.fractions).toHaveLength(1);
  });

  it('representa um item lancado com status tecnico (mesmo vocabulario do StatusPill)', () => {
    const item = orderItemResponseSchema.parse(buildItem({ status: 'IN_OVEN' }));
    expect(item.status).toBe('IN_OVEN');
  });

  it('valida o envelope { item } da repeticao (US-028 §7)', () => {
    const response = repeatOrderItemResponseSchema.parse({
      item: buildItem({ repeatedFromItemId: itemId, unitPrice: '55.00', totalPrice: '55.00' }),
    });
    expect(response.item.repeatedFromItemId).toBe(itemId);
    expect(response.item.unitPrice).toBe('55.00');
  });

  it('valida o consumo da sessao com subtotal/taxa/total e item cancelado riscado', () => {
    const response = sessionConsumptionResponseSchema.parse({
      items: [
        {
          orderItemId: itemId,
          orderId,
          name: 'Pizza G Mussarela',
          quantity: 1,
          unitPrice: '52.00',
          total: '52.00',
          status: 'READY',
          statusLabel: 'A caminho',
          etaMinutes: null,
          cancelled: false,
          variantId,
          productAvailable: true,
        },
      ],
      subtotal: '52.00',
      serviceFee: '5.20',
      serviceFeeOptional: true,
      total: '57.20',
      openedAt: '2026-08-02T20:12:04.221Z',
      minutesOpen: 47,
    });

    expect(response.serviceFeeOptional).toBe(true);
    expect(response.items[0]?.statusLabel).toBe('A caminho');
  });

  it('valida a mensagem de tempo real (SignalR ou fallback de polling, ADR-011)', () => {
    const event = tableConsumptionEventSchema.parse({
      type: 'order.item.ready',
      data: { orderItemId: itemId, productName: 'Pizza G Mussarela' },
    });
    expect(event.type).toBe('order.item.ready');
  });
});
