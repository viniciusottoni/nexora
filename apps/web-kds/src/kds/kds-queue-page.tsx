import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { OrderTicket, SyncStatus, type OperationalRequestIdentity } from '@nexora/ui';
import type { KdsQueueItem } from '@nexora/contracts';
import { playAlertChime, vibrateAlert } from '../notifications/alert-sound.js';
import { readStationIdFromAccessToken } from './decode-station-claim.js';
import { KdsQueueApi } from './kds-queue-api.js';
import { createKdsHubConnection, KdsRealtimeClient, type KdsConnectionMode } from './kds-realtime.js';
import { formatRelativeSync, toOrderTicketChannel } from './kds-signals.js';
import './kds-queue-page.css';

export interface KdsQueuePageProps {
  /** Vazio no edge (mesma origem) — parâmetro só para permitir apontar para outro host em teste. */
  readonly baseUrl?: string;
  readonly identity: Readonly<OperationalRequestIdentity>;
}

function channelWhereFallback(channel: KdsQueueItem['channel']): string {
  switch (channel) {
    case 'Delivery':
    case 'Marketplace':
      return 'Delivery';
    case 'Takeout':
      return 'Balcão';
    case 'DineIn':
    default:
      return 'Salão';
  }
}

/**
 * Fila do KDS por praça (US-031) — lê `GET /v1/kds/queue`, reage ao `KdsHub` via SignalR com
 * fallback de polling a cada 5 s e recuperação por `Resume` na reconexão (ADR-011). A praça vem da
 * claim `stn` do próprio token (nunca escolhida na tela — ver docstring de
 * `readStationIdFromAccessToken`), então não há nenhum seletor de praça aqui: um terminal só vê a
 * fila da praça a que está fisicamente associado.
 *
 * [DECISÃO DE ESCOPO] Não é a renderização completa da US-040 (arrastar para status seguinte,
 * agrupamento por tempo, densidade configurável) — o objetivo desta história é provar o
 * ROTEAMENTO: o pedido confirmado aparece aqui em tempo real, sobrevive a uma queda de WebSocket e
 * distingue canal salão/delivery (critério de aceite).
 */
export function KdsQueuePage({ baseUrl = '', identity }: Readonly<KdsQueuePageProps>) {
  const stationId = useMemo(() => readStationIdFromAccessToken(identity.accessToken), [identity.accessToken]);
  const [items, setItems] = useState<readonly KdsQueueItem[] | null>(null);
  const [error, setError] = useState<string>();
  const [connectionMode, setConnectionMode] = useState<KdsConnectionMode>('ws');
  const [lastSyncAt, setLastSyncAt] = useState<Date>();
  const [now, setNow] = useState(() => new Date());

  const api = useMemo(() => new KdsQueueApi(baseUrl), [baseUrl]);
  const lastEventIdRef = useRef<string | undefined>(undefined);
  const knownItemIdsRef = useRef<Set<string>>(new Set());
  // Ref (não state) de propósito: só marca "já tivemos uma carga bem-sucedida" para decidir se um
  // item novo merece som/vibração — não pode entrar nas deps de `refresh` (abaixo), ou cada
  // resposta recriaria o callback e disparia o efeito de carga inicial de novo (loop de fetch).
  const hasLoadedOnceRef = useRef(false);

  const refresh = useCallback(async () => {
    if (!stationId) return;
    try {
      const response = await api.queue(identity, stationId, lastEventIdRef.current);

      // Som/vibração só quando um item REALMENTE novo chega — nunca a cada poll de 5s enquanto a
      // fila não mudou (US-031 §10: "sinal sonoro... a cozinha não fica olhando a tela", não "a
      // cada 5 segundos"). Na primeira carga não soa: são itens que já estavam na fila antes
      // deste terminal conectar, não uma chegada nova.
      const hasNewItem = response.items.some((item) => !knownItemIdsRef.current.has(item.orderItemId));
      if (hasNewItem && hasLoadedOnceRef.current) {
        vibrateAlert();
        playAlertChime();
      }
      hasLoadedOnceRef.current = true;

      knownItemIdsRef.current = new Set(response.items.map((item) => item.orderItemId));
      lastEventIdRef.current = response.lastEventId;
      setItems(response.items);
      setLastSyncAt(new Date());
      setError(undefined);
    } catch {
      // US-031 §9 (comportamento offline): sem rede, mantém a última fila conhecida em vez de
      // limpar a tela — a cozinha continua vendo o que já sabia, só não mais em tempo real.
      setError('Sem conexão com o servidor local — mostrando a última fila conhecida.');
    }
  }, [api, identity, stationId]);

  // Ref para a versão mais recente de `refresh` — o efeito da conexão realtime (abaixo) só
  // reconecta o WebSocket se accessToken/stationId mudarem, nunca por causa desta closure.
  const refreshRef = useRef(refresh);
  useEffect(() => {
    refreshRef.current = refresh;
  }, [refresh]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    if (!stationId) return;
    const connection = createKdsHubConnection(`${baseUrl}/hubs/kds`, () => identity.accessToken);
    const realtime = new KdsRealtimeClient(connection, {
      onEvent: () => {
        void refreshRef.current();
      },
      onModeChange: setConnectionMode,
      poll: () => refreshRef.current(),
      getLastEventId: () => lastEventIdRef.current,
    });
    void realtime.start();
    return () => {
      void realtime.stop();
    };
  }, [baseUrl, identity.accessToken, stationId]);

  // Relógio de 1s só para o cronômetro de cada ticket e o rótulo "há Ns" do SyncStatus — não
  // dispara nenhum fetch.
  useEffect(() => {
    const interval = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(interval);
  }, []);

  if (!stationId) {
    return (
      <p className="kds-queue__no-station nx-anim-in" role="status">
        Este terminal não está associado a nenhuma praça de produção. Peça a um gerente para
        configurar a praça deste dispositivo.
      </p>
    );
  }

  return (
    <main className="kds-queue" data-surface="kds">
      <header className="kds-queue__header">
        <h1>Fila da praça</h1>
        <SyncStatus
          state={connectionMode === 'ws' ? 'online' : 'delayed'}
          {...(lastSyncAt ? { lastSync: formatRelativeSync(lastSyncAt, now) } : {})}
        />
      </header>

      {error ? (
        <p className="kds-queue__error nx-anim-in" role="alert">
          {error}
        </p>
      ) : null}

      {items === null ? (
        <p className="kds-queue__loading" role="status">
          Carregando fila…
        </p>
      ) : items.length === 0 ? (
        <p className="kds-queue__empty" role="status">
          Cozinha em dia — nenhum pedido na fila.
        </p>
      ) : (
        // nx-stagger só afeta a MONTAGEM inicial (animation em nó não remontado não reinicia) —
        // itens chegando via SignalR entram um a um em <li>/<article> novos, cada um com
        // nx-anim-in próprio (dado pelo componente base db-order-ticket, packages/ui).
        <div className="kds-queue__grid nx-stagger">
          {items.map((item) => (
            <OrderTicket
              key={item.orderItemId}
              data-channel={item.channel}
              data-testid="kds-ticket"
              code={item.orderCode}
              where={item.table ? `Mesa ${item.table}` : channelWhereFallback(item.channel)}
              channel={toOrderTicketChannel(item.channel)}
              seconds={Math.max(0, Math.round((now.getTime() - new Date(item.placedAt).getTime()) / 1000))}
              items={[
                {
                  qty: item.quantity,
                  name: item.productName,
                  modifiers: [item.notes, ...item.modifiers].filter(Boolean).join(' · ') || undefined,
                },
              ]}
            />
          ))}
        </div>
      )}
    </main>
  );
}
