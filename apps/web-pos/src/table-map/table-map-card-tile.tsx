import { memo } from 'react';
import { Badge, Button, TableCard } from '@nexora/ui';
import type { TableMapEntry, TableMapStatus } from '@nexora/contracts';
import { formatMinutesOpen, selectTopSignals, toTableCardStatus } from './table-map-signals.js';

export interface TableMapCardTileProps {
  readonly id: string;
  readonly label: string;
  readonly status: TableMapStatus;
  readonly minutesOpen: number | null;
  readonly guestCount: number | null;
  readonly totalLabel: string | null;
  readonly waiterName: string | null;
  readonly sessionId: string | null;
  readonly billRequested: boolean;
  readonly waiterCalled: boolean;
  readonly itemsReadyToServe: number;
  readonly aboveAvgDuration: boolean;
  readonly onSelect?: (id: string) => void;
  /** US-025 §7 — garçom confirma que atendeu a chamada, some o indicador do mapa. */
  readonly onAcknowledgeCall?: (tableId: string) => void;
  /** US-026 §4, cenário "Solicitação pelo garçom" — marca a mesa como "conta solicitada" direto do mapa (modo SINGLE, um toque). */
  readonly onRequestBill?: (sessionId: string) => void;
  /** US-027 §10 — abre a tela de divisão da conta quando a mesa já pediu a conta (`billRequested`). */
  readonly onOpenBilling?: (sessionId: string) => void;
}

/**
 * Cartão de uma mesa no mapa (US-023). Envolto em `memo` de propósito e recebendo só props
 * primitivas (não o objeto `TableMapEntry` inteiro, que é recriado a cada `GET /v1/tables` mesmo
 * quando o conteúdo não muda) — é isso que faz a comparação rasa do `memo` funcionar de verdade:
 * das 60 mesas do orçamento de desempenho (US-023 §12), normalmente só um punhado muda entre uma
 * atualização e outra, e as demais nem re-renderizam. Ver `table-map-page.test.tsx` para o teste
 * que prova isso (contagem de renders com props idênticas).
 */
function TableMapCardTileComponent({
  id,
  label,
  status,
  minutesOpen,
  guestCount,
  totalLabel,
  waiterName,
  sessionId,
  billRequested,
  waiterCalled,
  itemsReadyToServe,
  aboveAvgDuration,
  onSelect,
  onAcknowledgeCall,
  onRequestBill,
  onOpenBilling,
}: Readonly<TableMapCardTileProps>) {
  // Reconstrução mínima só para reaproveitar a mesma lógica de prioridade de sinais do backend
  // (selectTopSignals/urgencyScore) — não afeta a memoização acima, que já decidiu se este corpo
  // sequer executa com base nas props primitivas.
  const entryLike: TableMapEntry = {
    id,
    label,
    area: '',
    status,
    seats: 0, // não usado por selectTopSignals/toTableCardStatus — só preenche o tipo TableMapEntry
    session: null,
    flags: { billRequested, waiterCalled, itemsReadyToServe, aboveAvgDuration },
  };
  const signals = selectTopSignals(entryLike);
  const attention = billRequested || waiterCalled || itemsReadyToServe > 0;

  return (
    <div className="table-map__tile">
      <TableCard
        name={`Mesa ${label}`}
        status={toTableCardStatus(status)}
        {...(minutesOpen != null ? { elapsed: formatMinutesOpen(minutesOpen) } : {})}
        {...(guestCount != null ? { guests: guestCount } : {})}
        {...(totalLabel != null ? { total: totalLabel } : {})}
        {...(waiterName != null ? { waiter: waiterName } : {})}
        attention={attention}
        onClick={onSelect ? () => onSelect(id) : undefined}
      />
      {signals.length > 0 ? (
        <div className="table-map__signals" aria-label="Sinais de ação pendente">
          {signals.map((signal) => (
            <Badge key={signal.key} tone={signal.key === 'billRequested' ? 'danger' : 'warning'} icon={signal.icon} size="sm">
              {signal.label}
            </Badge>
          ))}
        </div>
      ) : null}

      {waiterCalled && onAcknowledgeCall ? (
        <Button size="sm" variant="secondary" onClick={() => onAcknowledgeCall(id)}>
          Atendido
        </Button>
      ) : null}

      {status !== 'FREE' && !billRequested && sessionId && onRequestBill ? (
        <Button size="sm" variant="ghost" onClick={() => onRequestBill(sessionId)}>
          Pedir conta
        </Button>
      ) : null}

      {billRequested && sessionId && onOpenBilling ? (
        <Button size="sm" variant="primary" onClick={() => onOpenBilling(sessionId)}>
          Dividir a conta
        </Button>
      ) : null}
    </div>
  );
}

export const TableMapCardTile = memo(TableMapCardTileComponent);
