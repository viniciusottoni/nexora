import { describe, expect, it } from 'vitest';
import {
  addOrderItemRequestSchema,
  createOrderRequestSchema,
  createOrderResponseSchema,
  createPublicOrderRequestSchema,
  orderItemResponseSchema,
  orderResponseSchema,
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

describe('contratos de criacao de pedido (US-030 §7)', () => {
  const sessionId = '0198aabb-7777-7000-8000-000000000007';

  it('valida o corpo minimo de POST /v1/orders (garcom/POS, canal e mesa explicitos)', () => {
    const request = createOrderRequestSchema.parse({
      channel: 'DineIn',
      sessionId,
      items: [{ variantId, quantity: 1 }],
    });
    expect(request.channel).toBe('DineIn');
    expect(request.items).toHaveLength(1);
  });

  it('recusa pedido sem nenhum item', () => {
    expect(() =>
      createOrderRequestSchema.parse({ channel: 'DineIn', sessionId, items: [] }),
    ).toThrow();
  });

  it('aceita item com modificadores, fracoes (meio a meio) e observacao livre', () => {
    const request = createOrderRequestSchema.parse({
      channel: 'DineIn',
      sessionId,
      items: [
        {
          variantId,
          quantity: 1,
          notes: 'bem assada, sem cebola',
          modifiers: [{ modifierId: itemId, quantity: 1 }],
          fractions: [
            { variantId, weight: 0.5 },
            { variantId: orderId, weight: 0.5 },
          ],
        },
      ],
    });
    expect(request.items[0]?.notes).toBe('bem assada, sem cebola');
    expect(request.items[0]?.fractions).toHaveLength(2);
  });

  it('valida o corpo de POST /v1/public/orders (cliente na mesa, sem channel/sessionId)', () => {
    const request = createPublicOrderRequestSchema.parse({ items: [{ variantId, quantity: 2 }] });
    expect(request.items[0]?.quantity).toBe(2);
  });

  it('valida o envelope { order, promisedAt, estimatedMinutes } do pedido confirmado, com codigo curto', () => {
    const response = createOrderResponseSchema.parse({
      order: {
        id: orderId,
        shortCode: 'A47',
        status: 'PLACED',
        sessionId,
        channel: 'DineIn',
        total: '60.00',
        placedAt: '2026-07-31T20:47:12.334Z',
        items: [buildItem()],
      },
      promisedAt: '2026-07-31T20:59:00Z',
      estimatedMinutes: 12,
    });
    expect(response.order.shortCode).toBe('A47');
    expect(response.order.status).toBe('PLACED');
    expect(response.estimatedMinutes).toBe(12);
  });

  it('recusa status de pedido fora do vocabulario conhecido', () => {
    expect(() =>
      orderResponseSchema.parse({
        id: orderId,
        shortCode: 'A47',
        status: 'PENDING',
        sessionId: null,
        channel: 'DineIn',
        total: '60.00',
        placedAt: null,
        items: [],
      }),
    ).toThrow();
  });
});
