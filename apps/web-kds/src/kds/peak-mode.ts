/**
 * US-047 §4/§10 — cálculo puro do modo pico AUTOMÁTICO (histerese na troca de modo). Lógica de
 * negócio isolada de React de propósito: é o coração testável desta história ("histerese impede
 * oscilação entre modos", §12). A composição com a ativação/desativação MANUAL do operador (que
 * tem prioridade sobre esta função — §10) vive em `use-peak-mode.ts`.
 */

/** Contagem é de CARTÕES/pedidos na fila (`orderGroups.length`), não de itens — §2 "30 pedidos pendentes". */
export interface PeakModeThresholds {
  readonly thresholdItems: number;
  readonly hysteresisItems: number;
}

/** Valores sugeridos pelo contrato de API da história (§7): `thresholdItems: 20, hysteresisItems: 5`. */
export const DEFAULT_PEAK_MODE_THRESHOLDS: PeakModeThresholds = {
  thresholdItems: 20,
  hysteresisItems: 5,
};

/**
 * Decide se o modo pico deve estar ativo dado o tamanho atual da fila, aplicando histerese:
 * ativa ao ATINGIR `thresholdItems` (`orderCount >= thresholdItems`), mas só desativa quando a
 * fila cair ABAIXO de `thresholdItems - hysteresisItems` — nunca no mesmo ponto em que ativou.
 * Com o exemplo do contrato (limiar 20, histerese 5): ativa em 20, permanece ativo de 19 até 15,
 * e só desativa ao cair para 14.
 *
 * Desativação usa limite ESTRITO (`<`, nunca `<=`) de propósito: é o que torna a função estável
 * mesmo no caso degenerado `hysteresisItems === 0` (histerese desligada). Nesse caso as duas
 * metades — "ativa quando `orderCount >= thresholdItems`" e "permanece ativa enquanto
 * `orderCount >= thresholdItems - hysteresisItems`" — colapsam no MESMO predicado
 * (`thresholdItems - 0 === thresholdItems`), então repetir a chamada com `currentlyActive` já
 * atualizado (é assim que o hook chamador usa esta função: o resultado de uma chamada vira a
 * entrada `currentlyActive` da próxima) nunca produz um resultado diferente para o mesmo
 * `orderCount` parado. Com `<=` em vez de `<`, `orderCount === thresholdItems` oscilaria a cada
 * novo cálculo — ativa (`>=` bate) → o mesmo valor cruza a condição de desativar (`<=` também
 * bate) → desativa → o valor ainda bate `>=` → ativa de novo — mesmo sem a fila mudar de tamanho.
 */
export function resolvePeakMode(
  currentlyActive: boolean,
  orderCount: number,
  thresholdItems: number,
  hysteresisItems: number,
): boolean {
  if (currentlyActive) {
    return orderCount >= thresholdItems - hysteresisItems;
  }
  return orderCount >= thresholdItems;
}
