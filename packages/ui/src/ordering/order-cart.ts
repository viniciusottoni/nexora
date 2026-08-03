import type { CreateOrderItemRequest } from '@nexora/contracts';
import type { ComposableFractionFlavor, ComposableModifier, ComposableProduct } from './composable-menu.js';

/**
 * US-030 (Criar pedido com itens, modificadores e frações) §10 — estado/matemática do carrinho de
 * composição, compartilhado por `web-pos` (garçom) e `web-menu` (cliente pela mesa). Dinheiro
 * sempre em CENTAVOS inteiros aqui dentro (ADR-017: `double`/`float` proibidos para dinheiro) —
 * só convertido de/para a string decimal do contrato nas bordas (`moneyToCents`/`centsToMoney`).
 */
export function moneyToCents(value: string): number {
  const negative = value.startsWith('-');
  const unsigned = negative ? value.slice(1) : value;
  const [whole, fraction = '0'] = unsigned.split('.');
  const cents = Number(whole || '0') * 100 + Number((fraction + '00').slice(0, 2));
  return negative ? -cents : cents;
}

export function centsToMoney(cents: number): string {
  const negative = cents < 0;
  const abs = Math.round(Math.abs(cents));
  const whole = Math.floor(abs / 100);
  const fraction = (abs % 100).toString().padStart(2, '0');
  return `${negative ? '-' : ''}${whole}.${fraction}`;
}

/** "R$ 45,90" — mesmo formato de `table-map-signals.ts#formatMoneyBrl`, aqui a partir de centavos. */
export function formatCentsBrl(cents: number): string {
  return (cents / 100).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

/**
 * Divide 1,0 em `count` pesos iguais truncados em 4 casas, sobra na primeira parcela (ADR-017:
 * "toda divisão concilia") — mesmo algoritmo de `apps/web-menu/src/fraction-builder/fraction-builder.tsx#splitEqualWeights`,
 * reproduzido aqui (não importado) porque aquele módulo é local a `web-menu` e este é
 * compartilhado por dois apps — ver docstring de `composable-menu.ts` sobre a decisão de não criar
 * um pacote novo para isto.
 */
export function splitEqualWeights(count: number): number[] {
  if (count <= 0) return [];
  const base = Math.floor((1 / count) * 10000) / 10000;
  const weights = new Array<number>(count).fill(base);
  const remainder = Number((1 - base * count).toFixed(4));
  weights[0] = Number((weights[0]! + remainder).toFixed(4));
  return weights;
}

export interface CartLineModifierSelection {
  readonly modifierId: string;
  readonly name: string;
  readonly quantity: number;
  readonly priceDeltaCents: number;
}

export interface CartLineFractionSelection {
  readonly variantId: string;
  readonly name: string;
  readonly weight: number;
  readonly priceCents: number;
}

/** Uma linha do carrinho/comanda — uma escolha completa (produto + variante + modificadores + fração + observação). */
export interface CartLine {
  /** Id local (crypto.randomUUID), só para `key`/remoção — nunca enviado ao servidor. */
  readonly localId: string;
  readonly productId: string;
  readonly productName: string;
  readonly variantId: string;
  readonly quantity: number;
  readonly notes: string;
  /** Preço unitário BASE (variante escolhida, ou média dos sabores quando é fração) — sem modificadores. */
  readonly unitPriceCents: number;
  readonly modifiers: readonly CartLineModifierSelection[];
  readonly fractions: readonly CartLineFractionSelection[];
}

/** Preço de uma unidade do item, incluindo modificadores — o que multiplica pela quantidade. */
export function lineUnitTotalCents(line: Pick<CartLine, 'unitPriceCents' | 'modifiers'>): number {
  const modifiersCents = line.modifiers.reduce(
    (sum, modifier) => sum + modifier.priceDeltaCents * modifier.quantity,
    0,
  );
  return line.unitPriceCents + modifiersCents;
}

export function lineTotalCents(line: CartLine): number {
  return lineUnitTotalCents(line) * line.quantity;
}

/** Soma de todas as linhas — é isto que a UI mostra como "preço total sempre visível" (US-030 §10). */
export function cartTotalCents(lines: readonly CartLine[]): number {
  return lines.reduce((sum, line) => sum + lineTotalCents(line), 0);
}

/** Preço médio dos sabores escolhidos — estimativa client-side; o servidor recalcula pela regra vigente (RN-009). */
export function averageFractionPriceCents(flavors: readonly ComposableFractionFlavor[]): number {
  if (flavors.length === 0) return 0;
  const total = flavors.reduce((sum, flavor) => sum + moneyToCents(flavor.price), 0);
  return Math.round(total / flavors.length);
}

/** Monta o payload real de `POST /v1/orders`/`POST /v1/public/orders` a partir do carrinho (US-030 §7). */
export function buildCreateOrderItems(lines: readonly CartLine[]): CreateOrderItemRequest[] {
  return lines.map((line) => ({
    variantId: line.variantId,
    quantity: line.quantity,
    notes: line.notes.trim().length > 0 ? line.notes.trim() : null,
    modifiers:
      line.modifiers.length > 0
        ? line.modifiers.map((modifier) => ({ modifierId: modifier.modifierId, quantity: modifier.quantity }))
        : null,
    fractions:
      line.fractions.length > 0
        ? line.fractions.map((fraction) => ({ variantId: fraction.variantId, weight: fraction.weight }))
        : null,
  }));
}

export interface ModifierGroupValidationError {
  readonly groupId: string;
  readonly groupName: string;
}

/**
 * Cenário Gherkin "Grupo de modificadores obrigatório pendente" (US-030 §4) — validação de
 * PRÉ-envio (o servidor valida de novo e é a fonte da verdade final, `MODIFIER_GROUP_REQUIRED`/
 * `MODIFIER_GROUP_SELECTION_INVALID`): confere se cada grupo do produto tem uma seleção dentro de
 * `minSelect..maxSelect`, e se todo grupo obrigatório tem ao menos 1 escolha.
 */
export function findModifierGroupValidationError(
  product: Pick<ComposableProduct, 'modifierGroups'>,
  selectedModifiers: ReadonlyMap<string, number>,
): ModifierGroupValidationError | null {
  for (const group of product.modifierGroups) {
    const selectedCount = group.modifiers.reduce(
      (count, modifier) => count + (selectedModifiers.has(modifier.id) ? 1 : 0),
      0,
    );
    const requiredMin = group.isRequired ? Math.max(1, group.minSelect) : group.minSelect;
    if (selectedCount < requiredMin) {
      return { groupId: group.id, groupName: group.name };
    }
    if (group.maxSelect > 0 && selectedCount > group.maxSelect) {
      return { groupId: group.id, groupName: group.name };
    }
  }
  return null;
}

export function toCartLineModifiers(
  modifiers: readonly ComposableModifier[],
  selected: ReadonlyMap<string, number>,
): CartLineModifierSelection[] {
  return modifiers
    .filter((modifier) => selected.has(modifier.id))
    .map((modifier) => ({
      modifierId: modifier.id,
      name: modifier.name,
      quantity: selected.get(modifier.id) ?? 1,
      priceDeltaCents: moneyToCents(modifier.priceDelta),
    }));
}

/**
 * Meta de erro `{ itemIndex, groupId, groupName }`/`{ itemIndex, variantId }` (US-030 §7) já
 * convertida pelo backend em `problem.meta` (`ResultExtensions.ExtractMeta`) — a mesma forma para
 * os dois lados (POST /v1/orders e POST /v1/public/orders).
 */
export interface OrderValidationErrorMeta {
  readonly itemIndex?: number;
  readonly groupId?: string;
  readonly groupName?: string;
  readonly variantId?: string;
}

/**
 * US-030 §10 — "erro de validação apontando o item e o campo exatos, nunca mensagem genérica".
 * Traduz `code`/`meta` do ProblemDetails (ADR-021) numa frase em português que nomeia o item (pela
 * posição no carrinho, já que é a mesma ordem enviada) e o campo exato.
 */
export function describeOrderValidationError(
  code: string | undefined,
  meta: Readonly<OrderValidationErrorMeta> | undefined,
  lines: readonly CartLine[],
): string {
  const itemIndex = meta?.itemIndex;
  const line = itemIndex !== undefined ? lines[itemIndex] : undefined;
  const itemLabel =
    itemIndex === undefined
      ? null
      : line
        ? `Item ${itemIndex + 1} (${line.productName})`
        : `Item ${itemIndex + 1}`;

  switch (code) {
    case 'MODIFIER_GROUP_REQUIRED': {
      const groupName = meta?.groupName ?? 'obrigatório';
      return itemLabel
        ? `${itemLabel}: selecione uma opção do grupo "${groupName}" antes de enviar.`
        : `Selecione uma opção do grupo "${groupName}" antes de enviar.`;
    }
    case 'MODIFIER_GROUP_SELECTION_INVALID': {
      const groupName = meta?.groupName ?? 'modificador';
      return itemLabel
        ? `${itemLabel}: a quantidade escolhida no grupo "${groupName}" não é válida.`
        : `A quantidade escolhida no grupo "${groupName}" não é válida.`;
    }
    case 'PRODUCT_UNAVAILABLE':
      return itemLabel
        ? `${itemLabel} ficou indisponível — remova-o do pedido e tente novamente.`
        : 'Um item ficou indisponível — remova-o do pedido e tente novamente.';
    case 'ORDER_NOT_ACCEPTING_ITEMS':
      return 'Este pedido não aceita mais itens agora — chame o garçom.';
    default:
      return 'Não foi possível confirmar o pedido agora. Tente novamente.';
  }
}
