import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Badge,
  Button,
  Card,
  EmptyState,
  Icon,
  Input,
  SegmentedControl,
  StatTile,
  StatusPill,
  SyncStatus,
  type OperationalRequestIdentity,
} from '@nexora/ui';
import type { OpenSessionEntry, OpenSessionsSortBy, OpenSessionsSummary } from '@nexora/contracts';
import {
  createTableMapHubConnection,
  TableMapRealtimeClient,
  type TableMapConnectionMode,
} from '../table-map/table-map-realtime.js';
import { formatMinutesOpen, formatMoneyBrl, formatRelativeSync } from '../table-map/table-map-signals.js';
import { formatPendingItems, formatSessionsSubtitle, formatWaitingSince } from './cash-panel-signals.js';
import { CashPanelApi } from './cash-panel-api.js';
import './cash-panel-page.css';

export interface CashPanelPageProps {
  /** Vazio no edge (mesma origem) — parâmetro só para permitir apontar para outro host em teste. */
  readonly baseUrl?: string;
  readonly identity: Readonly<OperationalRequestIdentity>;
  /** US-050 §3.2 "Fora desta história": montar a conta é a US-051 — este painel só abre a tela quando já existe uma. */
  readonly onOpenBilling?: (sessionId: string) => void;
}

const SORT_OPTIONS = [
  { value: 'urgency', label: 'Urgência' },
  { value: 'table', label: 'Nº da mesa' },
] as const;

/** Evita uma requisição por tecla digitada — 300 ms é imperceptível para quem digita, mas poupa o edge de N chamadas por busca (US-050 §10: "busca com foco automático, para operação por teclado"). */
const SEARCH_DEBOUNCE_MS = 300;

/**
 * Painel do caixa (US-050) — segunda "visão" das mesmas sessões de mesa do mapa do garçom
 * (US-023, `table-map/table-map-page.tsx`), com o foco do caixa: densidade máxima (tabela, não
 * cartões), prioridade a conta solicitada, busca por mesa/comanda, totalizador do salão. Reage ao
 * MESMO hub SignalR do mapa de mesas (a fonte de dados é a mesma tabela `table_session`) — não abre
 * uma segunda conexão WebSocket para o mesmo canal, com o mesmo fallback de polling (ADR-011).
 */
export function CashPanelPage({ baseUrl = '', identity, onOpenBilling }: Readonly<CashPanelPageProps>) {
  const [sessions, setSessions] = useState<readonly OpenSessionEntry[] | null>(null);
  const [summary, setSummary] = useState<OpenSessionsSummary>();
  const [error, setError] = useState<string>();
  const [searchInput, setSearchInput] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const [sortBy, setSortBy] = useState<OpenSessionsSortBy>('urgency');
  const [connectionMode, setConnectionMode] = useState<TableMapConnectionMode>('ws');
  const [lastSyncAt, setLastSyncAt] = useState<Date>();
  const [now, setNow] = useState(() => new Date());

  const api = useMemo(() => new CashPanelApi(baseUrl), [baseUrl]);
  const searchInputRef = useRef<HTMLInputElement>(null);

  // US-050 §10: o caixa começa a digitar assim que a tela abre, sem precisar clicar no campo.
  useEffect(() => {
    searchInputRef.current?.focus();
  }, []);

  useEffect(() => {
    const timeout = setTimeout(() => setAppliedSearch(searchInput), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  const refresh = useCallback(async () => {
    try {
      const response = await api.listOpenSessions(identity, { ...(appliedSearch ? { search: appliedSearch } : {}), sortBy });
      setSessions(response.sessions);
      setSummary(response.summary);
      setLastSyncAt(new Date());
      setError(undefined);
    } catch {
      // US-050 §9 (comportamento offline): sem rede, mantém o último painel conhecido — o caixa
      // continua vendo dado correto, só não mais em tempo real (mesmo padrão de table-map-page.tsx).
      setError('Sem conexão com o servidor local — mostrando o último painel conhecido.');
    }
  }, [api, identity, appliedSearch, sortBy]);

  // Ref para a versão mais recente de `refresh` — o efeito da conexão realtime não pode depender
  // de `appliedSearch`/`sortBy` diretamente, ou reconectaria o WebSocket a cada busca/troca de
  // ordenação (mesmo raciocínio de table-map-page.tsx).
  const refreshRef = useRef(refresh);
  useEffect(() => {
    refreshRef.current = refresh;
  }, [refresh]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    const connection = createTableMapHubConnection(`${baseUrl}/hubs/table-map`, () => identity.accessToken);
    const realtime = new TableMapRealtimeClient(connection, {
      onTableChanged: () => {
        void refreshRef.current();
      },
      onModeChange: setConnectionMode,
      poll: () => refreshRef.current(),
    });
    void realtime.start();
    return () => {
      void realtime.stop();
    };
  }, [baseUrl, identity.accessToken]);

  // Relógio de 1s só para o rótulo "há Ns" do SyncStatus — não dispara nenhum fetch.
  useEffect(() => {
    const interval = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(interval);
  }, []);

  const hasSearch = appliedSearch.trim().length > 0;

  return (
    <main className="cash-panel">
      <header className="cash-panel__header">
        <div>
          <p className="cash-panel__eyebrow">Painel do caixa</p>
          <h1>Mesas e comandas abertas</h1>
          <SyncStatus
            state={connectionMode === 'ws' ? 'online' : 'delayed'}
            {...(lastSyncAt ? { lastSync: formatRelativeSync(lastSyncAt, now) } : {})}
          />
        </div>
        {summary ? (
          <div className="cash-panel__summary nx-anim-in">
            <StatTile label="Sessões abertas" value={summary.openSessions} icon="table_restaurant" />
            <StatTile
              label="Total em aberto"
              value={formatMoneyBrl(summary.totalOpen)}
              icon="payments"
              variant="pulse"
            />
          </div>
        ) : null}
      </header>

      <div className="cash-panel__controls">
        <Input
          ref={searchInputRef}
          type="search"
          placeholder="Buscar por mesa ou comanda…"
          icon={<Icon name="search" size={18} />}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          aria-label="Buscar por mesa ou comanda"
        />
        <SegmentedControl
          options={SORT_OPTIONS}
          value={sortBy}
          onChange={(value) => setSortBy(value as OpenSessionsSortBy)}
          size="lg"
        />
      </div>

      {error ? (
        <p className="cash-panel__error nx-anim-in" role="alert">
          {error}
        </p>
      ) : null}

      <Card
        title="Salão"
        subtitle={sessions ? formatSessionsSubtitle(sessions.length, hasSearch) : undefined}
      >
        {sessions === null ? (
          <p className="cash-panel__loading" role="status">
            Carregando painel do caixa…
          </p>
        ) : sessions.length === 0 ? (
          <EmptyState
            icon={hasSearch ? 'search_off' : 'table_restaurant'}
            title={hasSearch ? 'Nenhuma mesa ou comanda encontrada' : 'Nenhuma mesa aberta no momento'}
          >
            {hasSearch ? 'Tente buscar por outro número de mesa ou comanda.' : 'O salão está livre — nenhuma sessão aberta agora.'}
          </EmptyState>
        ) : (
          <div className="db-table-wrap">
            <table className="db-table db-table--compact cash-panel__table">
              <thead>
                <tr>
                  <th>Mesa</th>
                  <th>Área</th>
                  <th>Status</th>
                  <th>Aberta há</th>
                  <th>Pessoas</th>
                  <th>Garçom</th>
                  <th style={{ textAlign: 'right' }}>Total</th>
                  <th>Pendências</th>
                  <th>Ações</th>
                </tr>
              </thead>
              {/* nx-stagger só afeta a MONTAGEM inicial da tabela (animation em nó não remontado não
                  reinicia) — atualizações via SignalR trocam o conteúdo de linhas já existentes. */}
              <tbody className="nx-stagger">
                {sessions.map((session) => (
                  <tr
                    key={session.sessionId}
                    className={
                      session.status === 'BILL_REQUESTED' ? 'cash-panel__row cash-panel__row--attention' : 'cash-panel__row'
                    }
                  >
                    <td>
                      {session.table}
                      {session.orderCode ? <span className="cash-panel__order-code">{session.orderCode}</span> : null}
                    </td>
                    <td>{session.area}</td>
                    <td>
                      <StatusPill status={session.status} />
                      {session.status === 'BILL_REQUESTED' && session.waitingSeconds != null ? (
                        <span className="cash-panel__waiting">{formatWaitingSince(session.waitingSeconds)}</span>
                      ) : null}
                    </td>
                    <td>{formatMinutesOpen(session.minutesOpen)}</td>
                    <td>{session.guestCount}</td>
                    <td>{session.waiter?.name ?? '—'}</td>
                    <td className="db-table__numeric">{formatMoneyBrl(session.total)}</td>
                    <td>
                      {session.pendingItems > 0 ? (
                        <Badge tone="warning" size="sm">
                          {formatPendingItems(session.pendingItems)}
                        </Badge>
                      ) : (
                        <span className="cash-panel__no-pending">—</span>
                      )}
                    </td>
                    <td>
                      {session.status === 'BILL_REQUESTED' && onOpenBilling ? (
                        <Button size="sm" variant="primary" onClick={() => onOpenBilling(session.sessionId)}>
                          Dividir a conta
                        </Button>
                      ) : (
                        <span className="cash-panel__no-pending">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </main>
  );
}
