import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { SegmentedControl, SyncStatus, Switch, type OperationalRequestIdentity } from '@nexora/ui';
import type { TableMapEntry, TableMapSortBy } from '@nexora/contracts';
import { playAlertChime, vibrateAlert } from '../notifications/alert-sound.js';
import { AlertsRealtimeClient, createAlertsHubConnection, type AlertPayload } from '../notifications/alerts-realtime.js';
import { TableMapCardTile } from './table-map-card-tile.js';
import { TableMapApi } from './table-map-api.js';
import { createTableMapHubConnection, TableMapRealtimeClient, type TableMapConnectionMode } from './table-map-realtime.js';
import { formatMoneyBrl, formatRelativeSync, urgencyScore } from './table-map-signals.js';
import './table-map-page.css';

export interface TableMapPageProps {
  /** Vazio no edge (mesma origem) — parâmetro só para permitir apontar para outro host em teste. */
  readonly baseUrl?: string;
  readonly identity: Readonly<OperationalRequestIdentity>;
  /** Selecionado quando o garçom liga "minhas mesas" — comparado a `session.waiter.id` (US-023 §4). */
  readonly onSelectTable?: (tableId: string) => void;
  /** US-027 §10 — abre a tela de divisão da conta a partir do card de uma mesa com `billRequested=true`. */
  readonly onOpenBilling?: (sessionId: string) => void;
}

const SORT_OPTIONS = [
  { value: 'urgency', label: 'Urgência' },
  { value: 'label', label: 'Nº da mesa' },
] as const;

/**
 * Mapa de mesas (US-023) — tela inicial do garçom/caixa, não item de menu (§10). Lê
 * `GET /v1/tables`, reage ao `TableMapHub` via WebSocket com fallback de polling a cada 5 s
 * (ADR-011) e agrupa por ambiente com grade de cartões grandes.
 */
export function TableMapPage({ baseUrl = '', identity, onSelectTable, onOpenBilling }: Readonly<TableMapPageProps>) {
  const [entries, setEntries] = useState<readonly TableMapEntry[] | null>(null);
  const [error, setError] = useState<string>();
  const [mineOnly, setMineOnly] = useState(false);
  const [sortBy, setSortBy] = useState<TableMapSortBy>('urgency');
  const [connectionMode, setConnectionMode] = useState<TableMapConnectionMode>('ws');
  const [lastSyncAt, setLastSyncAt] = useState<Date>();
  const [now, setNow] = useState(() => new Date());

  const api = useMemo(() => new TableMapApi(baseUrl), [baseUrl]);

  const [actionError, setActionError] = useState<string>();
  const [waiterCallToast, setWaiterCallToast] = useState<string>();

  useEffect(() => {
    if (!waiterCallToast) return;
    const timeout = setTimeout(() => setWaiterCallToast(undefined), 6000);
    return () => clearTimeout(timeout);
  }, [waiterCallToast]);

  const refresh = useCallback(async () => {
    try {
      const response = await api.list(identity, { mine: mineOnly, sortBy });
      setEntries(response.tables);
      setLastSyncAt(new Date());
      setError(undefined);
    } catch {
      // US-023 §9 (comportamento offline): sem rede, mantém o último mapa conhecido em vez de
      // limpar a tela — o garçom continua vendo dado correto, só não mais em tempo real (a
      // ausência de atualização em SyncStatus já é a indicação discreta do estado degradado).
      setError('Sem conexão com o servidor local — mostrando o último mapa conhecido.');
    }
  }, [api, identity, mineOnly, sortBy]);

  // US-025 §7: garçom confirma que atendeu a chamada direto do mapa — resolve o alerta e recarrega
  // para o indicador desaparecer (o mesmo GET que já é a fonte da verdade do mapa).
  const handleAcknowledgeCall = useCallback(
    async (tableId: string) => {
      setActionError(undefined);
      try {
        await api.acknowledgeWaiterCall(identity, tableId);
        await refresh();
      } catch (cause) {
        setActionError(cause instanceof Error ? cause.message : 'Não foi possível confirmar o atendimento agora.');
      }
    },
    [api, identity, refresh],
  );

  // US-026 §4, cenário "Solicitação pelo garçom" — um toque, modo SINGLE (valor único): a escolha
  // fina de divisão continua sendo do cliente pelo cardápio (WaiterCallPanel, apps/web-menu); esta
  // ação do mapa é o atalho rápido "marcar como conta solicitada" que a história também exige.
  const handleRequestBill = useCallback(
    async (sessionId: string) => {
      setActionError(undefined);
      try {
        await api.requestBill(identity, sessionId, 'SINGLE');
        await refresh();
      } catch (cause) {
        setActionError(cause instanceof Error ? cause.message : 'Não foi possível pedir a conta agora.');
      }
    },
    [api, identity, refresh],
  );

  // Ref para a versão mais recente de `refresh` — o efeito da conexão realtime (abaixo) não pode
  // depender de `mineOnly`/`sortBy` diretamente, ou reconectaria o WebSocket a cada troca de
  // filtro. O polling/push sempre chama a versão corrente através do ref.
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
    // Deps propositalmente restritas a baseUrl/accessToken (não inclui mineOnly/sortBy nem a
    // própria `refresh`) — ver o comentário do `refreshRef` acima: só reconecta o WebSocket se o
    // dispositivo mudar de token, nunca por causa de um filtro trocado.
  }, [baseUrl, identity.accessToken]);

  // US-025 §10: canal SEPARADO do mapa (ver docstring de AlertsRealtimeClient) — só dispara
  // vibração/som/aviso quando ESTE dispositivo é o alvo (sala role:waiter/user:{id} do próprio
  // token, resolvida no servidor); o mapa em si já mostra o badge via GET /v1/tables.
  useEffect(() => {
    const connection = createAlertsHubConnection(`${baseUrl}/hubs/alerts`, () => identity.accessToken);
    const realtime = new AlertsRealtimeClient(connection, {
      onAlert: (alert: AlertPayload) => {
        if (alert.type !== 'table.waiter_called') return;
        vibrateAlert();
        playAlertChime();
        const label = typeof alert.data.label === 'string' ? alert.data.label : '?';
        setWaiterCallToast(`Mesa ${label} está chamando você!`);
        void refreshRef.current();
      },
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

  const displayEntries = useMemo(() => {
    if (!entries) return entries;
    if (sortBy !== 'urgency') return entries;
    // Reordena defensivamente pelo mesmo critério do servidor (GetTableMapQueryHandler.UrgencyScore)
    // — protege contra uma resposta que chegue com a ordenação de uma requisição anterior atrasada.
    return [...entries].sort((a, b) => urgencyScore(b) - urgencyScore(a));
  }, [entries, sortBy]);

  const groups = useMemo(() => {
    if (!displayEntries) return [];
    const byArea = new Map<string, TableMapEntry[]>();
    for (const entry of displayEntries) {
      const list = byArea.get(entry.area);
      if (list) list.push(entry);
      else byArea.set(entry.area, [entry]);
    }
    return [...byArea.entries()];
  }, [displayEntries]);

  return (
    <main className="table-map">
      <header className="table-map__header">
        <div>
          <h1>Mapa de mesas</h1>
          <SyncStatus
            state={connectionMode === 'ws' ? 'online' : 'delayed'}
            {...(lastSyncAt ? { lastSync: formatRelativeSync(lastSyncAt, now) } : {})}
          />
        </div>
        <div className="table-map__controls">
          <Switch
            label="Minhas mesas"
            checked={mineOnly}
            onChange={(event) => setMineOnly(event.target.checked)}
          />
          <SegmentedControl
            options={SORT_OPTIONS}
            value={sortBy}
            onChange={(value) => setSortBy(value as TableMapSortBy)}
            size="lg"
          />
        </div>
      </header>

      {error ? (
        <p className="table-map__error" role="alert">
          {error}
        </p>
      ) : null}

      {actionError ? (
        <p className="table-map__error" role="alert">
          {actionError}
        </p>
      ) : null}

      {waiterCallToast ? (
        <p className="table-map__waiter-call-toast" role="alert">
          {waiterCallToast}
        </p>
      ) : null}

      {displayEntries === null ? (
        <p className="table-map__loading" role="status">
          Carregando mapa de mesas…
        </p>
      ) : displayEntries.length === 0 ? (
        <p className="table-map__empty" role="status">
          {mineOnly ? 'Nenhuma mesa sob sua responsabilidade no momento.' : 'Nenhuma mesa cadastrada.'}
        </p>
      ) : (
        groups.map(([area, tables]) => (
          <section key={area} className="table-map__area" aria-label={area}>
            <h2>{area}</h2>
            <div className="table-map__grid">
              {tables.map((entry) => (
                <TableMapCardTile
                  key={entry.id}
                  id={entry.id}
                  label={entry.label}
                  status={entry.status}
                  minutesOpen={entry.session?.minutesOpen ?? null}
                  guestCount={entry.session?.guestCount ?? null}
                  totalLabel={entry.session ? formatMoneyBrl(entry.session.total) : null}
                  waiterName={entry.session?.waiter?.name ?? null}
                  sessionId={entry.session?.sessionId ?? null}
                  billRequested={entry.flags.billRequested}
                  waiterCalled={entry.flags.waiterCalled}
                  itemsReadyToServe={entry.flags.itemsReadyToServe}
                  aboveAvgDuration={entry.flags.aboveAvgDuration}
                  onAcknowledgeCall={handleAcknowledgeCall}
                  onRequestBill={handleRequestBill}
                  {...(onOpenBilling ? { onOpenBilling } : {})}
                  {...(onSelectTable && entry.status === 'FREE' ? { onSelect: onSelectTable } : {})}
                />
              ))}
            </div>
          </section>
        ))
      )}
    </main>
  );
}
