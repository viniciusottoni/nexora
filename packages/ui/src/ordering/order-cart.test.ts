import { describe, expect, it } from 'vitest';
import type { ComposableProduct } from './composable-menu.js';
import {
  buildCreateOrderItems,
  cartTotalCents,
  centsToMoney,
  type CartLine,
  describeOrderValidationError,
  findModifierGroupValidationError,
  formatCentsBrl,
  lineTotalCents,
  moneyToCents,
  splitEqualWeights,
  toCartLineModifiers,
} from './order-cart.js';

const variantId = '0198aabb-6666-7000-8000-000000000006';
const groupId = '0198aabb-4444-7000-8000-000000000004';
const modifierId = '0198aabb-5555-7000-8000-000000000005';

function line(overrides: Partial<CartLine> = {}): CartLine {
  return {
    localId: 'local-1',
    productId: 'product-1',
    productName: 'Pizza Grande',
    variantId,
    quantity: 1,
    notes: '',
    unitPriceCents: 5200,
    modifiers: [],
    fractions: [],
    ...overrides,
  };
}

describe('dinheiro em centavos (ADR-017)', () => {
  it('converte string decimal para centavos e volta sem perder precisao', () => {
    expect(moneyToCents('45.90')).toBe(4590);
    expect(moneyToCents('8.00')).toBe(800);
    expect(centsToMoney(4590)).toBe('45.90');
    expect(centsToMoney(800)).toBe('8.00');
  });

  it('formata em BRL a partir de centavos', () => {
    expect(formatCentsBrl(4590)).toContain('45,90');
  });
});

describe('total do carrinho — preco sempre visivel, atualizado a cada escolha (US-030 §10)', () => {
  it('soma quantidade x (preco base + modificadores)', () => {
    const withModifier = line({
      quantity: 2,
      modifiers: [{ modifierId, name: 'Catupiry', quantity: 1, priceDeltaCents: 800 }],
    });
    // (52.00 + 8.00) * 2 = 120.00
    expect(lineTotalCents(withModifier)).toBe(12000);
  });

  it('atualiza o total do carrinho a cada linha adicionada', () => {
    const lines = [line({ localId: 'a' }), line({ localId: 'b', quantity: 2, unitPriceCents: 3000 })];
    expect(cartTotalCents(lines)).toBe(5200 + 2 * 3000);

    const withThird = [...lines, line({ localId: 'c', unitPriceCents: 1000 })];
    expect(cartTotalCents(withThird)).toBe(cartTotalCents(lines) + 1000);
  });
});

describe('fracoes (meio a meio) — pesos iguais somam 1,0 (RN-009)', () => {
  it('divide em 2 pesos iguais', () => {
    expect(splitEqualWeights(2)).toEqual([0.5, 0.5]);
  });

  it('divide em 3 pesos com a sobra na primeira parcela, soma exatamente 1', () => {
    const weights = splitEqualWeights(3);
    expect(weights).toHaveLength(3);
    expect(weights.reduce((sum, weight) => sum + weight, 0)).toBeCloseTo(1, 4);
    expect(weights[0]).toBeGreaterThanOrEqual(weights[1]!);
  });

  it('monta o payload de fracoes com variantId e weight de cada sabor escolhido', () => {
    const fractionLine = line({
      fractions: [
        { variantId: 'mussarela-g', name: 'Mussarela', weight: 0.5, priceCents: 4500 },
        { variantId: 'calabresa-g', name: 'Calabresa', weight: 0.5, priceCents: 4800 },
      ],
    });
    const [item] = buildCreateOrderItems([fractionLine]);
    expect(item?.fractions).toEqual([
      { variantId: 'mussarela-g', weight: 0.5 },
      { variantId: 'calabresa-g', weight: 0.5 },
    ]);
  });
});

describe('observacao livre por item (US-030 §4)', () => {
  it('envia a observacao aparada, e null quando vazia', () => {
    const [withNote] = buildCreateOrderItems([line({ notes: '  bem assada, sem cebola  ' })]);
    expect(withNote?.notes).toBe('bem assada, sem cebola');

    const [withoutNote] = buildCreateOrderItems([line({ notes: '   ' })]);
    expect(withoutNote?.notes).toBeNull();
  });
});

describe('grupo de modificador obrigatorio pendente (US-030 §4)', () => {
  const product: Pick<ComposableProduct, 'modifierGroups'> = {
    modifierGroups: [
      {
        id: groupId,
        name: 'Tamanho',
        minSelect: 1,
        maxSelect: 1,
        isRequired: true,
        modifiers: [
          { id: modifierId, name: 'Grande', priceDelta: '0.00' },
          { id: 'outro-modificador', name: 'Pequena', priceDelta: '-10.00' },
        ],
      },
    ],
  };

  it('bloqueia quando nenhuma opcao do grupo obrigatorio foi escolhida', () => {
    const error = findModifierGroupValidationError(product, new Map());
    expect(error).toEqual({ groupId, groupName: 'Tamanho' });
  });

  it('libera quando a escolha respeita min/max', () => {
    const error = findModifierGroupValidationError(product, new Map([[modifierId, 1]]));
    expect(error).toBeNull();
  });

  it('bloqueia quando a quantidade escolhida excede o maximo do grupo', () => {
    const error = findModifierGroupValidationError(
      product,
      new Map([[modifierId, 1], ['outro-modificador', 1]]),
    );
    expect(error).toEqual({ groupId, groupName: 'Tamanho' });
  });

  it('converte a selecao em linhas do carrinho com o preco de cada modificador', () => {
    const selected = toCartLineModifiers(product.modifierGroups[0]!.modifiers, new Map([[modifierId, 1]]));
    expect(selected).toEqual([{ modifierId, name: 'Grande', quantity: 1, priceDeltaCents: 0 }]);
  });
});

describe('erro de validacao aponta o item e o campo exatos, nunca mensagem generica (US-030 §10)', () => {
  const lines = [line({ productName: 'Pizza Grande' }), line({ productName: 'Refrigerante' })];

  it('MODIFIER_GROUP_REQUIRED nomeia o item e o grupo pendente', () => {
    const message = describeOrderValidationError(
      'MODIFIER_GROUP_REQUIRED',
      { itemIndex: 0, groupId, groupName: 'Tamanho' },
      lines,
    );
    expect(message).toBe('Item 1 (Pizza Grande): selecione uma opção do grupo "Tamanho" antes de enviar.');
  });

  it('PRODUCT_UNAVAILABLE nomeia o item pelo indice quando ele nao esta mais no carrinho', () => {
    const message = describeOrderValidationError('PRODUCT_UNAVAILABLE', { itemIndex: 1 }, [lines[0]!]);
    expect(message).toBe('Item 2 ficou indisponível — remova-o do pedido e tente novamente.');
  });

  it('codigo desconhecido cai numa mensagem generica de "tente novamente", nunca quebra', () => {
    const message = describeOrderValidationError('ALGO_NOVO', undefined, lines);
    expect(message).toBe('Não foi possível confirmar o pedido agora. Tente novamente.');
  });
});
