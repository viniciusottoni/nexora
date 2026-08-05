import type { KdsQueueItem } from '@nexora/contracts';

/**
 * US-043 (Contagem consolidada all-day) — agregação client-side pura, sem estado de rede próprio:
 * reflexo direto da fila (`KdsQueuePage.items`) que já chegou filtrada por praça e por status
 * ATIVO (a fila nunca traz SERVED/CANCELLED — ver docstring de `KdsQueueApi`).
 */
export interface AllDaySummaryEntry {
  /** Nome do produto/sabor consolidado — o `productName` do item sem fração, ou da fração. */
  readonly productName: string;
  /** Unidades pendentes, ponderadas por fração (US-043 §4, cenário "Contagem proporcional"). */
  readonly quantity: number;
}

/**
 * Soma `quantity` por produto. Item SEM fração conta `quantity` inteiro pro próprio
 * `productName`; item COM fração (meio a meio) reparte `quantity * Number(fraction.weight)` para
 * cada sabor da fração — cenário de aceite: "4 pedidos de meio a meio, todos com metade de
 * Mussarela → Mussarela conta 2, não 4". `weight` chega como string (ADR-017, mesmo tratamento de
 * dinheiro para não perder precisão em trânsito) — convertido aqui só para exibição agregada, não
 * para nenhum cálculo financeiro.
 */
export function computeAllDaySummary(items: readonly KdsQueueItem[]): AllDaySummaryEntry[] {
  const totals = new Map<string, number>();

  const add = (productName: string, amount: number) => {
    totals.set(productName, (totals.get(productName) ?? 0) + amount);
  };

  for (const item of items) {
    if (item.fractions.length === 0) {
      add(item.productName, item.quantity);
      continue;
    }
    for (const fraction of item.fractions) {
      add(fraction.productName, item.quantity * Number(fraction.weight));
    }
  }

  return [...totals.entries()]
    .map(([productName, quantity]) => ({ productName, quantity: roundQuantity(quantity) }))
    .sort((a, b) => b.quantity - a.quantity || a.productName.localeCompare(b.productName, 'pt-BR'));
}

/** Evita ruído de ponto flutuante (ex. 0.1 + 0.2) sem virar decisão monetária — só exibição. */
function roundQuantity(quantity: number): number {
  return Math.round(quantity * 100) / 100;
}

/** "12" para inteiro, "10.5" para fração — sem zeros à direita (US-043 §10, "números grandes, texto curto"). */
export function formatAllDayQuantity(quantity: number): string {
  const rounded = roundQuantity(quantity);
  if (Number.isInteger(rounded)) return String(rounded);
  return rounded.toFixed(2).replace(/0+$/, '').replace(/\.$/, '');
}
