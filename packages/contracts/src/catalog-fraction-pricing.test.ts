import { describe, expect, it } from 'vitest';
import {
  fractionPriceRuleSchema,
  previewFractionPricingRequestSchema,
  previewFractionPricingResponseSchema,
} from './catalog-fraction-pricing.js';

const mussarelaId = '0198aabb-6666-7000-8000-000000000001';
const calabresaId = '0198aabb-6666-7000-8000-000000000002';

describe('contratos de precificacao de fracao (US-013)', () => {
  it('valida o corpo do preview com duas fracoes', () => {
    const parsed = previewFractionPricingRequestSchema.parse({
      fractions: [
        { variantId: mussarelaId, weight: 0.5 },
        { variantId: calabresaId, weight: 0.5 },
      ],
    });

    expect(parsed.fractions).toHaveLength(2);
  });

  it('recusa uma unica fracao (nao configura meio a meio)', () => {
    expect(() =>
      previewFractionPricingRequestSchema.parse({
        fractions: [{ variantId: mussarelaId, weight: 1 }],
      }),
    ).toThrow('Um item meio a meio precisa de ao menos duas frações');
  });

  it('recusa peso fora da faixa (0, 1]', () => {
    expect(() =>
      previewFractionPricingRequestSchema.parse({
        fractions: [
          { variantId: mussarelaId, weight: 0 },
          { variantId: calabresaId, weight: 1 },
        ],
      }),
    ).toThrow('O peso da fração deve ser maior que zero');

    expect(() =>
      previewFractionPricingRequestSchema.parse({
        fractions: [
          { variantId: mussarelaId, weight: 1.5 },
          { variantId: calabresaId, weight: 0.5 },
        ],
      }),
    ).toThrow('O peso da fração não pode ultrapassar 1,0');
  });

  it('aceita canal opcional', () => {
    const parsed = previewFractionPricingRequestSchema.parse({
      fractions: [
        { variantId: mussarelaId, weight: 0.5 },
        { variantId: calabresaId, weight: 0.5 },
      ],
      channel: 'Delivery',
    });

    expect(parsed.channel).toBe('Delivery');
  });

  it('so aceita as tres regras conhecidas', () => {
    expect(fractionPriceRuleSchema.parse('HIGHEST')).toBe('HIGHEST');
    expect(fractionPriceRuleSchema.parse('AVERAGE')).toBe('AVERAGE');
    expect(fractionPriceRuleSchema.parse('PROPORTIONAL')).toBe('PROPORTIONAL');
    expect(() => fractionPriceRuleSchema.parse('MEDIANA')).toThrow();
  });

  it('valida a resposta do preview com a descricao composta', () => {
    const parsed = previewFractionPricingResponseSchema.parse({
      unitPrice: 52,
      priceRule: 'HIGHEST',
      description: 'G · Mussarela / Calabresa',
      fractions: [
        { variantId: mussarelaId, weight: 0.5, unitPrice: 45 },
        { variantId: calabresaId, weight: 0.5, unitPrice: 52 },
      ],
    });

    expect(parsed.description).toBe('G · Mussarela / Calabresa');
    expect(parsed.unitPrice).toBe(52);
  });
});
