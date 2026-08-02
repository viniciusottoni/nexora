import { describe, expect, it } from 'vitest';
import {
  bulkAdjustPricesRequestSchema,
  bulkAdjustPricesResponseSchema,
  setVariantChannelPriceRequestSchema,
  variantPriceTableResponseSchema,
} from './catalog-price-adjustments.js';

const variantId = '0198aabb-5555-7000-8000-000000000001';
const productId = '0198aabb-4444-7000-8000-000000000001';
const categoryId = '0198aabb-3333-7000-8000-000000000001';

describe('contratos de preco por canal de venda (US-014)', () => {
  it('valida a tabela de preco por canal com heranca marcada', () => {
    const parsed = variantPriceTableResponseSchema.parse({
      variantId,
      productId,
      channels: [
        {
          channel: 'DineIn',
          amount: '45.00',
          isInherited: false,
          validFrom: '2026-01-01T00:00:00Z',
        },
        {
          channel: 'Delivery',
          amount: '52.00',
          isInherited: false,
          validFrom: '2026-01-01T00:00:00Z',
        },
        {
          channel: 'Takeout',
          amount: '45.00',
          isInherited: true,
          validFrom: '2026-01-01T00:00:00Z',
        },
        { channel: 'Marketplace', amount: null, isInherited: false, validFrom: null },
      ],
    });

    expect(parsed.channels).toHaveLength(4);
    expect(parsed.channels[2]?.isInherited).toBe(true);
  });

  it('recusa tabela com quantidade de canais diferente de quatro', () => {
    expect(() =>
      variantPriceTableResponseSchema.parse({
        variantId,
        productId,
        channels: [{ channel: 'DineIn', amount: '45.00', isInherited: false, validFrom: null }],
      }),
    ).toThrow();
  });

  it('recusa valor monetario fora do formato "0.00"', () => {
    expect(() =>
      setVariantChannelPriceRequestSchema.parse({ prices: [{ channel: 'DineIn', amount: '45' }] }),
    ).toThrow('Valor monetário inválido');
  });

  it('recusa lista de precos vazia', () => {
    expect(() => setVariantChannelPriceRequestSchema.parse({ prices: [] })).toThrow(
      'Informe ao menos um preço por canal',
    );
  });

  it('aceita definir dois canais na mesma chamada', () => {
    const parsed = setVariantChannelPriceRequestSchema.parse({
      prices: [
        { channel: 'DineIn', amount: '46.00' },
        { channel: 'Delivery', amount: '53.00' },
      ],
    });

    expect(parsed.prices).toHaveLength(2);
  });

  it('recusa reajuste que reduziria o preco abaixo de zero', () => {
    expect(() =>
      bulkAdjustPricesRequestSchema.parse({ categoryId, channel: 'Delivery', percent: -150 }),
    ).toThrow('O reajuste não pode reduzir o preço abaixo de zero');
  });

  it('aceita reajuste de -100% (zera o preco, permitido pelo dominio)', () => {
    const parsed = bulkAdjustPricesRequestSchema.parse({
      categoryId,
      channel: 'Delivery',
      percent: -100,
    });
    expect(parsed.percent).toBe(-100);
  });

  it('valida a resposta do reajuste em massa', () => {
    const parsed = bulkAdjustPricesResponseSchema.parse({
      updated: 20,
      effectiveFrom: '2026-08-02T12:00:00Z',
    });
    expect(parsed.updated).toBe(20);
  });
});
