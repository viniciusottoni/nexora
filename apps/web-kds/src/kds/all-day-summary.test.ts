import type { KdsQueueItem } from '@nexora/contracts';
import { describe, expect, it } from 'vitest';
import { computeAllDaySummary, formatAllDayQuantity } from './all-day-summary.js';

let seq = 0;

/** Fábrica de item mínima — só os campos que `computeAllDaySummary` de fato lê variam por teste. */
function makeItem(overrides: Partial<KdsQueueItem> = {}): KdsQueueItem {
  seq += 1;
  return {
    orderItemId: `item-${seq}`,
    orderId: `order-${seq}`,
    orderCode: `A${seq}`,
    productId: `product-${seq}`,
    productName: 'Pizza G Mussarela',
    quantity: 1,
    modifiers: [],
    notes: null,
    status: 'QUEUED',
    placedAt: new Date().toISOString(),
    elapsedSeconds: 0,
    thresholdState: 'NORMAL',
    warnSeconds: 300,
    criticalSeconds: 600,
    table: null,
    channel: 'DineIn',
    fractions: [],
    ...overrides,
  };
}

describe('computeAllDaySummary (US-043)', () => {
  it('consolida por produto quando não há fração', () => {
    const items = Array.from({ length: 12 }, () => makeItem({ productName: 'Pizza G Mussarela', quantity: 1 }));

    const summary = computeAllDaySummary(items);

    expect(summary).toEqual([{ productName: 'Pizza G Mussarela', quantity: 12 }]);
  });

  it('conta fração proporcionalmente — 4 pedidos de meio a meio com metade de Mussarela contam 2, não 4', () => {
    const items = Array.from({ length: 4 }, () =>
      makeItem({
        productName: 'Pizza G Meio a Meio',
        quantity: 1,
        fractions: [
          { productName: 'Mussarela', weight: '0.5' },
          { productName: 'Calabresa', weight: '0.5' },
        ],
      }),
    );

    const summary = computeAllDaySummary(items);

    const mussarela = summary.find((entry) => entry.productName === 'Mussarela');
    const calabresa = summary.find((entry) => entry.productName === 'Calabresa');
    expect(mussarela?.quantity).toBe(2);
    expect(calabresa?.quantity).toBe(2);
    // Item de fração não deve deixar resíduo sob o próprio nome do produto combinado.
    expect(summary.find((entry) => entry.productName === 'Pizza G Meio a Meio')).toBeUndefined();
  });

  it('soma quantidade > 1 por item ponderada pela fração', () => {
    const items = [
      makeItem({
        productName: 'Pizza M Meio a Meio',
        quantity: 3,
        fractions: [
          { productName: 'Frango Catupiry', weight: '0.5' },
          { productName: 'Portuguesa', weight: '0.5' },
        ],
      }),
    ];

    const summary = computeAllDaySummary(items);

    expect(summary.find((entry) => entry.productName === 'Frango Catupiry')?.quantity).toBe(1.5);
    expect(summary.find((entry) => entry.productName === 'Portuguesa')?.quantity).toBe(1.5);
  });

  it('ordena por quantidade pendente decrescente', () => {
    const items = [
      ...Array.from({ length: 3 }, () => makeItem({ productName: 'Pizza P Calabresa' })),
      ...Array.from({ length: 8 }, () => makeItem({ productName: 'Pizza G Mussarela' })),
      ...Array.from({ length: 5 }, () => makeItem({ productName: 'Pizza M Portuguesa' })),
    ];

    const summary = computeAllDaySummary(items);

    expect(summary.map((entry) => entry.productName)).toEqual([
      'Pizza G Mussarela',
      'Pizza M Portuguesa',
      'Pizza P Calabresa',
    ]);
  });

  it('desempata alfabeticamente quando a quantidade é igual', () => {
    const items = [makeItem({ productName: 'Zebra' }), makeItem({ productName: 'Abacaxi' })];

    const summary = computeAllDaySummary(items);

    expect(summary.map((entry) => entry.productName)).toEqual(['Abacaxi', 'Zebra']);
  });

  it('fila vazia retorna lista vazia', () => {
    expect(computeAllDaySummary([])).toEqual([]);
  });

  it('produtos distintos com o mesmo nome de fração acumulam no mesmo total', () => {
    const items = [
      makeItem({
        productName: 'Pizza G Meio a Meio',
        quantity: 1,
        fractions: [{ productName: 'Mussarela', weight: '0.5' }, { productName: 'Calabresa', weight: '0.5' }],
      }),
      makeItem({ productName: 'Mussarela', quantity: 2, fractions: [] }),
    ];

    const summary = computeAllDaySummary(items);

    expect(summary.find((entry) => entry.productName === 'Mussarela')?.quantity).toBe(2.5);
  });
});

describe('formatAllDayQuantity (US-043 §10 — números grandes, texto curto)', () => {
  it('formata inteiro sem casas decimais', () => {
    expect(formatAllDayQuantity(12)).toBe('12');
    expect(formatAllDayQuantity(0)).toBe('0');
  });

  it('formata fração sem zeros à direita', () => {
    expect(formatAllDayQuantity(2.5)).toBe('2.5');
    expect(formatAllDayQuantity(10.5)).toBe('10.5');
  });

  it('preserva duas casas quando necessário e não deixa ruído de ponto flutuante', () => {
    expect(formatAllDayQuantity(1.5 + 1.5 + 1.5 + 1.5 - 6)).toBe('0');
    expect(formatAllDayQuantity(0.1 + 0.2)).toBe('0.3');
  });
});
