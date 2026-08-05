import { Icon } from '@nexora/ui';
import type { KdsQueueItem } from '@nexora/contracts';
import { computeAllDaySummary, formatAllDayQuantity } from './all-day-summary.js';
import './all-day-panel.css';

export interface AllDayPanelProps {
  /**
   * Itens da fila JÁ filtrados por praça (US-042) e por status ativo (a fila nunca traz
   * SERVED/CANCELLED) — este componente não busca nada, só agrega o que recebe. Reflexo direto:
   * recalcula a cada render em que `items` mudar, sem estado próprio de rede.
   */
  readonly items: readonly KdsQueueItem[];
}

/**
 * US-043 (Contagem consolidada all-day) — painel lateral com a soma de itens pendentes por
 * produto/sabor em TODA a fila (não só o que está visível na tela), ordenado por quantidade
 * decrescente, com fração de meio a meio contada proporcionalmente (`computeAllDaySummary`).
 * Puro reflexo de `items`: sem `useEffect`, sem fetch — atualiza sozinho quando a fila muda.
 */
export function AllDayPanel({ items }: Readonly<AllDayPanelProps>) {
  const entries = computeAllDaySummary(items);

  return (
    <aside className="all-day-panel nx-anim-in" aria-label="Contagem consolidada all-day" data-surface="kds">
      <header className="all-day-panel__head">
        <Icon name="dinner_dining" size={22} aria-hidden="true" />
        <h2 className="all-day-panel__title">All-day</h2>
      </header>

      {entries.length === 0 ? (
        <p className="all-day-panel__empty" role="status" data-testid="all-day-panel-empty">
          Sem pendências
        </p>
      ) : (
        <ul className="all-day-panel__list nx-stagger" data-testid="all-day-panel-list">
          {entries.map((entry) => (
            <li key={entry.productName} className="all-day-panel__item">
              <span className="all-day-panel__name">{entry.productName}</span>
              <span className="all-day-panel__count">{formatAllDayQuantity(entry.quantity)}</span>
            </li>
          ))}
        </ul>
      )}
    </aside>
  );
}
