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
  TableCard,
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
  /** Ajusta os textos quando o painel funciona como porta de entrada do recebimento. */
  readonly mode?: 'overview' | 'receiving';
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
export function CashPanelPage({ baseUrl = '', identity, onOpenBilling, mode = 'overview' }: Readonly<CashPanelPageProps>) {
  const [sessions, setSessions] = useState<readonly OpenSessionEntry[] | null>(null);
  const [summary, setSummary] = useState<OpenSessionsSummary>();
  const [error, setError] = useState<string>();
  const [searchInput, setSearchInput] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const [sortBy, setSortBy] = useState<OpenSessionsSortBy>('urgency');
  const [connectionMode, setConnectionMode] = useState<TableMapConnectionMode>('ws');
  const [lastSyncAt, setLastSyncAt] = useState<Date>();
  const [now, setNow] = useState(() => new Date());
  const [selectedSessionId, setSelectedSessionId] = useState<string>();

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
  const selectedSession =
    sessions?.find((session) => session.sessionId === selectedSessionId) ??
    sessions?.find((session) => session.status === 'BILL_REQUESTED') ??
    sessions?.[0];

  return (
    <main className="cash-panel">
      <div className="cash-panel__workspace">
        <div className="cash-panel__main-column">
          {connectionMode !== 'ws' ? (
            <div className="cash-panel__status">
              <SyncStatus
                state="delayed"
                {...(lastSyncAt ? { lastSync: formatRelativeSync(lastSyncAt, now) } : {})}
              />
            </div>
          ) : null}

          {summary ? (
            <div className="cash-panel__summary nx-anim-in">
              <StatTile
                label="Sessões abertas"
                value={summary.openSessions}
                icon="table_restaurant"
                comparison="mesas e comandas no salão"
              />
              <StatTile
                label="Total em aberto"
                value={formatMoneyBrl(summary.totalOpen)}
                icon="payments"
                comparison="consumo das sessões abertas"
              />
            </div>
          ) : null}

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
            />
          </div>

          {error ? (
            <p className="cash-panel__error nx-anim-in" role="alert">
              {error}
            </p>
          ) : null}

          <Card
            title={mode === 'receiving' ? 'Selecione a conta para receber' : 'Mesas abertas'}
            subtitle={sessions ? formatSessionsSubtitle(sessions.length, hasSearch) : undefined}
            padding="tight"
            className="cash-panel__tables-card"
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
              <div className="cash-panel__table-grid nx-stagger">
                {sessions.map((session) => (
                  <TableCard
                    key={session.sessionId}
                    name={
                      <>
                        <span className="cash-panel__table-prefix">Mesa</span>{' '}
                        <span>{session.table}</span>
                      </>
                    }
                    status={session.status}
                    elapsed={formatMinutesOpen(session.minutesOpen)}
                    guests={session.guestCount}
                    total={formatMoneyBrl(session.total)}
                    {...(session.waiter ? { waiter: session.waiter.name } : {})}
                    attention={session.status === 'BILL_REQUESTED'}
                    aria-pressed={selectedSession?.sessionId === session.sessionId}
                    className={selectedSession?.sessionId === session.sessionId ? 'cash-panel__table-card--selected' : ''}
                    onClick={() => setSelectedSessionId(session.sessionId)}
                  />
                ))}
              </div>
            )}
          </Card>
        </div>

        {selectedSession ? (
          <Card
            className="cash-panel__account"
            title={`Conta · Mesa ${selectedSession.table}`}
            subtitle={`${selectedSession.guestCount} ${selectedSession.guestCount === 1 ? 'pessoa' : 'pessoas'} · ${formatMinutesOpen(selectedSession.minutesOpen)} · garçom ${selectedSession.waiter?.name ?? 'não informado'}`}
            actions={<StatusPill status={selectedSession.status} live={selectedSession.status === 'BILL_REQUESTED'} />}
            footer={
              <Button
                variant="primary"
                disabled={selectedSession.status !== 'BILL_REQUESTED' || !onOpenBilling}
                onClick={() => onOpenBilling?.(selectedSession.sessionId)}
              >
                <Icon name="point_of_sale" size={18} />
                {selectedSession.status === 'BILL_REQUESTED'
                  ? mode === 'receiving'
                    ? 'Receber conta'
                    : 'Dividir a conta'
                  : 'Aguardando pedido da conta'}
              </Button>
            }
          >
            <dl className="cash-panel__account-details">
              <div>
                <dt>Comanda</dt>
                <dd>{selectedSession.orderCode ?? 'Ainda sem código'}</dd>
              </div>
              <div>
                <dt>Área</dt>
                <dd>{selectedSession.area}</dd>
              </div>
              <div>
                <dt>Pendências</dt>
                <dd>
                  {selectedSession.pendingItems > 0 ? (
                    <Badge tone="warning" size="sm">
                      {formatPendingItems(selectedSession.pendingItems)}
                    </Badge>
                  ) : (
                    'Nenhuma'
                  )}
                </dd>
              </div>
            </dl>

            {selectedSession.status === 'BILL_REQUESTED' && selectedSession.waitingSeconds != null ? (
              <div className="cash-panel__bill-requested">
                <Icon name="notifications_active" size={20} />
                <div>
                  <strong>Conta solicitada</strong>
                  <span>{formatWaitingSince(selectedSession.waitingSeconds)}</span>
                </div>
              </div>
            ) : null}

            <div className="cash-panel__account-total">
              <span>Total em aberto</span>
              <strong>{formatMoneyBrl(selectedSession.total)}</strong>
            </div>
          </Card>
        ) : null}
      </div>
    </main>
  );
}
