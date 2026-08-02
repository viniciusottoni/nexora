import type { TableMapEntry, TableMapStatus } from '@nexora/contracts';

/** Vocabulário de status de `StatusPill`/`TableCard` (packages/ui) — diferente do contrato da API, que usa "OCCUPIED". */
export type TableCardStatus = 'FREE' | 'OPEN' | 'BILL_REQUESTED' | 'PAID' | 'READY' | 'CLOSED';

/**
 * Traduz o status combinado mesa+sessão do backend (US-023 §7) para o vocabulário que
 * `TableCard`/`StatusPill` já entendem. "RESERVED"/"BLOCKED" não têm state.dedicado ainda no
 * design system (só FREE/OPEN/BILL_REQUESTED/PAID/READY/CLOSED) — mapeados para "CLOSED" com
 * rótulo próprio ("Reservada"/"Bloqueada"), a leitura mais honesta disponível hoje ("mesa
 * indisponível para sentar", que é exatamente o que os dois significam), até uma história futura
 * estender o vocabulário do StatusPill.
 */
export function toTableCardStatus(status: TableMapStatus): TableCardStatus {
  switch (status) {
    case 'FREE':
      return 'FREE';
    case 'OCCUPIED':
      return 'OPEN';
    case 'BILL_REQUESTED':
      return 'BILL_REQUESTED';
    case 'RESERVED':
    case 'BLOCKED':
    default:
      return 'CLOSED';
  }
}

export interface TableSignal {
  readonly key: 'billRequested' | 'waiterCalled' | 'itemsReady' | 'aboveAvg';
  readonly icon: string;
  readonly label: string;
}

/**
 * Urgência = quantos sinais de ação pendente a mesa carrega, do mais para o menos crítico —
 * mesma escala de pesos do backend (GetTableMapQueryHandler.UrgencyScore). Mantida em espelho
 * aqui porque, entre um refresh completo e o próximo (§ "Atualização em tempo real"), a tela
 * precisa reordenar a grade sozinha assim que um evento chegar pelo WebSocket, sem esperar o
 * próximo GET.
 */
export function urgencyScore(entry: TableMapEntry): number {
  return (
    (entry.flags.billRequested ? 8 : 0) +
    (entry.flags.waiterCalled ? 4 : 0) +
    (entry.flags.itemsReadyToServe > 0 ? 2 : 0) +
    (entry.flags.aboveAvgDuration ? 1 : 0)
  );
}

/**
 * US-023 §15: "Limitar a três sinais simultâneos" — excesso de indicador torna o cartão ilegível
 * em movimento. Prioridade igual à urgência (conta pedida é sempre o sinal mais importante).
 */
export function selectTopSignals(entry: TableMapEntry, limit = 3): readonly TableSignal[] {
  const candidates: TableSignal[] = [];
  if (entry.flags.billRequested) {
    candidates.push({ key: 'billRequested', icon: 'receipt_long', label: 'Conta pedida' });
  }
  if (entry.flags.waiterCalled) {
    candidates.push({ key: 'waiterCalled', icon: 'notifications_active', label: 'Garçom chamado' });
  }
  if (entry.flags.itemsReadyToServe > 0) {
    candidates.push({
      key: 'itemsReady',
      icon: 'room_service',
      label: entry.flags.itemsReadyToServe === 1 ? '1 item pronto' : `${entry.flags.itemsReadyToServe} itens prontos`,
    });
  }
  if (entry.flags.aboveAvgDuration) {
    candidates.push({ key: 'aboveAvg', icon: 'hourglass_bottom', label: 'Acima do tempo médio' });
  }
  return candidates.slice(0, limit);
}

/** "42 min" — o mesmo formato de `elapsed` esperado por `TableCard`. */
export function formatMinutesOpen(minutesOpen: number): string {
  return `${minutesOpen} min`;
}

/** "R$ 186,40" a partir da string monetária do contrato (ADR-017, dinheiro como string em JSON). */
export function formatMoneyBrl(amount: string): string {
  const value = Number.parseFloat(amount);
  if (Number.isNaN(value)) return amount;
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

/** "há 4 s" / "há 12 min" — formato esperado por `SyncStatus.lastSync` (RF-OFF-05/RF-BI-14). */
export function formatRelativeSync(lastSyncAt: Date, now = new Date()): string {
  const seconds = Math.max(0, Math.round((now.getTime() - lastSyncAt.getTime()) / 1000));
  if (seconds < 60) return `há ${seconds} s`;
  return `há ${Math.round(seconds / 60)} min`;
}
