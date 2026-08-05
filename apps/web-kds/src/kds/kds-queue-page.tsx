import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { KdsQueueItem } from '@nexora/contracts';
import { Icon, OrderTicket, SyncStatus, type OperationalRequestIdentity } from '@nexora/ui';
import { AvailabilityApi } from '../availability/availability-api.js';
import { MarkUnavailableFromItem } from '../availability/mark-unavailable-from-item.js';
import { playAlertChime, vibrateAlert } from '../notifications/alert-sound.js';
import { readStationIdFromAccessToken } from './decode-station-claim.js';
import { KdsApiError, KdsQueueApi } from './kds-queue-api.js';
import { createKdsHubConnection, KdsRealtimeClient, type KdsConnectionMode } from './kds-realtime.js';
import { KdsHistoryPage } from './kds-history-page.js';
import { groupKdsQueueByStation, useMultiStationKdsQueue, type KdsQueueItemWithStation } from './kds-multi-station-queue.js';
import { formatItemName, formatRelativeSync, groupItemsByOrder, toOrderTicketChannel, type KdsOrderGroup } from './kds-signals.js';
import { NumericKeypad } from './numeric-keypad.js';
import { AllDayPanel } from './all-day-panel.js';
import { PeakModeBanner } from './peak-mode-banner.js';
import { SILENT_ALERT_FLASH_CLASS_NAME, SoundSettingsPanel, useDeviceSoundPreferences, useSoundAlerts } from './sound-preferences.js';
import { StationFilterBar, useStationFilter } from './station-filter.js';
import { usePeakMode } from './use-peak-mode.js';
import './kds-queue-page.css';

export interface KdsQueuePageProps {
  /** Vazio no edge (mesma origem) — parâmetro só para permitir apontar para outro host em teste. */
  readonly baseUrl?: string;
  readonly identity: Readonly<OperationalRequestIdentity>;
}

function channelWhereFallback(channel: string): string {
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

/** US-041 §4 — janela de desfazer, espelha `UndoKdsItemAdvanceCommandHandler.UndoWindow` no backend. */
const UNDO_WINDOW_MS = 10_000;
/** US-040 §3 ("Fila crescente ... cartões devem reduzir de tamanho") — a partir de quantos cartões a grade entra em modo compacto. */
const DENSE_QUEUE_THRESHOLD = 12;

interface QueueSection {
  readonly key: string;
  readonly label: string | null;
  readonly color: string | null;
  readonly orderGroups: readonly KdsOrderGroup[];
}

/**
 * Fila do KDS por praça (US-040 a US-047) — lê `GET /v1/kds/queue`, agrupa por PEDIDO (um cartão
 * por pedido, `groupItemsByOrder`), reage ao `KdsHub` via SignalR com fallback de polling a cada
 * 5 s e recuperação por `Resume` na reconexão (ADR-011, US-048). A praça OPERACIONAL (a que o
 * teclado numérico avança) vem sempre da claim `stn` do próprio token — o filtro de praça (US-042)
 * só decide o que é EXIBIDO, nunca o que o terminal pode avançar (avançar um pedido de outra praça
 * pelo teclado deste terminal não faz sentido físico: o operador não está lá para preparar).
 *
 * Avanço de estado é EXCLUSIVAMENTE pelo teclado numérico (`NumericKeypad`) — sem toque no cartão,
 * sem mouse em lugar nenhum da tela (US-041 §10), exceto os botões dedicados de configuração
 * (som, histórico, filtro de praça, marcar indisponível), que são ações OPERACIONAIS distintas do
 * ciclo de produção e não competem com essa exigência.
 */
export function KdsQueuePage({ baseUrl = '', identity }: Readonly<KdsQueuePageProps>) {
  const stationId = useMemo(() => readStationIdFromAccessToken(identity.accessToken), [identity.accessToken]);
  const [items, setItems] = useState<readonly KdsQueueItem[] | null>(null);
  const [error, setError] = useState<string>();
  const [connectionMode, setConnectionMode] = useState<KdsConnectionMode>('ws');
  const [lastSyncAt, setLastSyncAt] = useState<Date>();
  const [now, setNow] = useState(() => new Date());
  const [keypadError, setKeypadError] = useState<string>();
  const [keypadBusy, setKeypadBusy] = useState(false);
  const [undoTarget, setUndoTarget] = useState<{ itemId: string; expiresAt: number }>();
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [soundPanelOpen, setSoundPanelOpen] = useState(false);
  const [showHistory, setShowHistory] = useState(false);
  const [expandedOrderIds, setExpandedOrderIds] = useState<ReadonlySet<string>>(new Set());

  const api = useMemo(() => new KdsQueueApi(baseUrl), [baseUrl]);
  const availabilityApi = useMemo(() => new AvailabilityApi(baseUrl, undefined, identity.accessToken), [baseUrl, identity.accessToken]);
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

      // Som/vibração de PEDIDO NOVO só quando um item REALMENTE novo chega — nunca a cada poll de
      // 5s enquanto a fila não mudou (US-045 trata timbre/volume/modo silencioso; aqui só decide
      // QUANDO tocar). Na primeira carga não soa: são itens que já estavam na fila antes deste
      // terminal conectar.
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

  // Relógio de 1s: cronômetro de cada ticket, rótulo "há Ns" do SyncStatus e contagem regressiva
  // da janela de desfazer — não dispara nenhum fetch.
  useEffect(() => {
    const interval = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    if (!undoTarget) return;
    if (now.getTime() >= undoTarget.expiresAt) setUndoTarget(undefined);
  }, [now, undoTarget]);

  useEffect(() => {
    const handleChange = () => setIsFullscreen(Boolean(document.fullscreenElement));
    document.addEventListener('fullscreenchange', handleChange);
    return () => document.removeEventListener('fullscreenchange', handleChange);
  }, []);

  // US-042 — filtro de praça (oculto em `mode==='single'`, o caso comum de um terminal por praça).
  const stationFilter = useStationFilter({ identity, baseUrl });
  const multiStation = useMultiStationKdsQueue(
    identity,
    stationFilter.mode === 'single' ? [] : stationFilter.selectedStationIds,
    { baseUrl },
  );

  // US-045 — som de pedido novo (reaproveita playAlertChime/vibrateAlert acima) + atraso crítico.
  const { preferences: soundPreferences, updatePreferences: updateSoundPreferences, saving: savingSoundPreferences, error: soundPreferencesError } =
    useDeviceSoundPreferences(identity);
  const { silentFlashItemIds } = useSoundAlerts(items ?? [], soundPreferences);

  // US-047 — modo pico: conta CARTÕES (pedidos), não itens.
  const singleStationOrderGroups = useMemo(() => (items ? groupItemsByOrder(items) : []), [items]);
  const peakMode = usePeakMode({ orderCount: singleStationOrderGroups.length, identity, baseUrl });

  const toggleExpandedOrder = useCallback((orderId: string) => {
    setExpandedOrderIds((current) => {
      const next = new Set(current);
      if (next.has(orderId)) next.delete(orderId);
      else next.add(orderId);
      return next;
    });
  }, []);

  const toggleFullscreen = useCallback(() => {
    if (document.fullscreenElement) {
      void document.exitFullscreen();
    } else {
      void document.documentElement.requestFullscreen().catch(() => {
        // Modo quiosque é conveniência, não requisito bloqueante — navegador sem suporte/permissão
        // simplesmente mantém a tela normal.
      });
    }
  }, []);

  const handleKeypadError = useCallback((err: unknown) => {
    setKeypadError(err instanceof KdsApiError ? err.message : 'Não foi possível concluir a operação.');
  }, []);

  const handleSubmit = useCallback(
    async (code: string) => {
      if (!stationId || keypadBusy) return;
      setKeypadBusy(true);
      setKeypadError(undefined);
      try {
        const result = await api.advanceOrder(identity, code, stationId, false);
        const advancedItem = result.advanced[0];
        if (advancedItem) {
          setUndoTarget({ itemId: advancedItem.id, expiresAt: Date.now() + UNDO_WINDOW_MS });
        }
        await refresh();
      } catch (err) {
        handleKeypadError(err);
      } finally {
        setKeypadBusy(false);
      }
    },
    [api, handleKeypadError, identity, keypadBusy, refresh, stationId],
  );

  const handleSubmitBatch = useCallback(
    async (code: string) => {
      if (!stationId || keypadBusy) return;
      setKeypadBusy(true);
      setKeypadError(undefined);
      try {
        await api.advanceOrder(identity, code, stationId, true);
        setUndoTarget(undefined);
        await refresh();
      } catch (err) {
        handleKeypadError(err);
      } finally {
        setKeypadBusy(false);
      }
    },
    [api, handleKeypadError, identity, keypadBusy, refresh, stationId],
  );

  const handleUndo = useCallback(async () => {
    if (!undoTarget || keypadBusy) return;
    setKeypadBusy(true);
    setKeypadError(undefined);
    try {
      await api.undoItem(identity, undoTarget.itemId);
      setUndoTarget(undefined);
      await refresh();
    } catch (err) {
      setUndoTarget(undefined);
      handleKeypadError(err);
    } finally {
      setKeypadBusy(false);
    }
  }, [api, handleKeypadError, identity, keypadBusy, refresh, undoTarget]);

  if (showHistory) {
    return <KdsHistoryPage baseUrl={baseUrl} identity={identity} onClose={() => setShowHistory(false)} />;
  }

  if (!stationId) {
    return (
      <p className="kds-queue__no-station nx-anim-in" role="status">
        Este terminal não está associado a nenhuma praça de produção. Peça a um gerente para
        configurar a praça deste dispositivo.
      </p>
    );
  }

  // US-042 §4 ("agrupada por praça") — em modo múltiplas praças/supervisão, uma seção por praça
  // ativa; em modo praça única (o padrão), uma seção sem rótulo — comportamento idêntico ao de
  // antes desta história.
  const sections: readonly QueueSection[] =
    stationFilter.mode === 'single'
      ? [{ key: 'single', label: null, color: null, orderGroups: singleStationOrderGroups }]
      : (() => {
          const byStation = groupKdsQueueByStation(
            (multiStation.items ?? []) as readonly KdsQueueItemWithStation[],
            stationFilter.activeStations.map((station) => station.id),
          );
          return stationFilter.activeStations.map((station) => ({
            key: station.id,
            label: station.name,
            color: station.color,
            orderGroups: groupItemsByOrder(byStation.get(station.id) ?? []),
          }));
        })();

  const effectiveItems: readonly KdsQueueItem[] = stationFilter.mode === 'single' ? (items ?? []) : (multiStation.items ?? []);
  const totalOrderCount = sections.reduce((sum, section) => sum + section.orderGroups.length, 0);
  const isDense = totalOrderCount > DENSE_QUEUE_THRESHOLD;
  const isLoading = stationFilter.mode === 'single' ? items === null : multiStation.items === null;
  const combinedError = error ?? multiStation.error ?? stationFilter.error;

  function renderOrderGroups(orderGroups: readonly KdsOrderGroup[]) {
    return (
      <div className={`kds-queue__grid nx-stagger ${isDense ? 'kds-queue__grid--dense' : ''}`.trim()}>
        {orderGroups.map((group) => {
          const hasSilentFlash = group.items.some((item) => silentFlashItemIds.has(item.orderItemId));
          const peakModeActive = peakMode.active && !expandedOrderIds.has(group.orderId);
          return (
            <OrderTicket
              key={group.orderId}
              data-channel={group.channel}
              data-testid="kds-ticket"
              className={hasSilentFlash ? SILENT_ALERT_FLASH_CLASS_NAME : undefined}
              code={group.orderCode}
              where={group.table ? `Mesa ${group.table}` : channelWhereFallback(group.channel)}
              channel={toOrderTicketChannel(group.channel)}
              seconds={Math.max(0, Math.round((now.getTime() - new Date(group.oldestPlacedAt).getTime()) / 1000))}
              warnAt={group.warnSeconds}
              lateAt={group.criticalSeconds}
              items={group.items.map((item) => {
                const detail = [item.notes, ...item.modifiers].filter(Boolean).join(' · ') || undefined;
                return {
                  qty: item.quantity,
                  name: formatItemName(item),
                  modifiers: (
                    <>
                      {peakModeActive ? null : detail}
                      <MarkUnavailableFromItem
                        productId={item.productId}
                        productName={item.productName}
                        orderItemId={item.orderItemId}
                        api={availabilityApi}
                        onMarked={() => void refresh()}
                      />
                    </>
                  ),
                  done: item.status === 'READY',
                };
              })}
              footer={
                peakMode.active && group.items.some((item) => item.notes || item.modifiers.length > 0) ? (
                  <button
                    type="button"
                    className="kds-queue__ticket-expand"
                    onClick={(event) => {
                      event.stopPropagation();
                      toggleExpandedOrder(group.orderId);
                    }}
                  >
                    <Icon name={expandedOrderIds.has(group.orderId) ? 'expand_less' : 'more_horiz'} size={16} />
                    {expandedOrderIds.has(group.orderId) ? 'Ocultar observações' : 'Ver observações'}
                  </button>
                ) : undefined
              }
            />
          );
        })}
      </div>
    );
  }

  return (
    <main className="kds-queue" data-surface="kds">
      <header className="kds-queue__header">
        <h1>Fila da praça</h1>
        <div className="kds-queue__header-actions">
          <StationFilterBar filter={stationFilter} />
          <SyncStatus
            state={connectionMode === 'ws' ? 'online' : 'delayed'}
            {...(lastSyncAt ? { lastSync: formatRelativeSync(lastSyncAt, now) } : {})}
          />
          <button
            type="button"
            className={`kds-queue__fullscreen ${!soundPreferences.enabled ? 'kds-queue__fullscreen--muted' : ''}`.trim()}
            onClick={() => setSoundPanelOpen(true)}
            aria-label={soundPreferences.enabled ? 'Configurar som do KDS' : 'Configurar som do KDS (modo silencioso ativo)'}
          >
            <Icon name={soundPreferences.enabled ? 'volume_up' : 'volume_off'} size={22} />
          </button>
          <button
            type="button"
            className="kds-queue__fullscreen"
            onClick={() => setShowHistory(true)}
            aria-label="Ver histórico do turno"
          >
            <Icon name="history" size={22} />
          </button>
          <button
            type="button"
            className="kds-queue__fullscreen"
            onClick={toggleFullscreen}
            aria-label={isFullscreen ? 'Sair do modo quiosque' : 'Entrar em modo quiosque'}
            aria-pressed={isFullscreen}
          >
            <Icon name={isFullscreen ? 'fullscreen_exit' : 'fullscreen'} size={22} />
          </button>
        </div>
      </header>

      <PeakModeBanner active={peakMode.active} manuallyDisabled={peakMode.manuallyDisabled} onToggle={peakMode.toggle} />

      {combinedError ? (
        <p className="kds-queue__error nx-anim-in" role="alert">
          {combinedError}
        </p>
      ) : null}

      <div className="kds-queue__body">
        <div className="kds-queue__main">
          {isLoading ? (
            <p className="kds-queue__loading" role="status">
              Carregando fila…
            </p>
          ) : totalOrderCount === 0 ? (
            <p className="kds-queue__empty" role="status">
              Cozinha em dia — nenhum pedido na fila.
            </p>
          ) : (
            // nx-stagger só afeta a MONTAGEM inicial (animation em nó não remontado não reinicia) —
            // pedidos chegando via SignalR/polling entram um a um em <article> novos, cada um com
            // nx-anim-in próprio (dado pelo componente base db-order-ticket, packages/ui).
            sections.map((section) =>
              section.orderGroups.length === 0 ? null : (
                <section key={section.key} className="kds-queue__station-section">
                  {section.label ? (
                    <h2 className="kds-queue__station-section-title">
                      <span
                        className="kds-queue__station-section-dot"
                        style={section.color ? { background: section.color } : undefined}
                        aria-hidden="true"
                      />
                      {section.label}
                    </h2>
                  ) : null}
                  {renderOrderGroups(section.orderGroups)}
                </section>
              ),
            )
          )}
        </div>

        <AllDayPanel items={effectiveItems} />
      </div>

      <NumericKeypad
        onSubmit={(code) => void handleSubmit(code)}
        onSubmitBatch={(code) => void handleSubmitBatch(code)}
        onUndo={() => void handleUndo()}
        undoAvailable={Boolean(undoTarget)}
        error={keypadError}
        disabled={keypadBusy}
      />

      <SoundSettingsPanel
        open={soundPanelOpen}
        onClose={() => setSoundPanelOpen(false)}
        preferences={soundPreferences}
        onChange={updateSoundPreferences}
        saving={savingSoundPreferences}
        error={soundPreferencesError}
      />
    </main>
  );
}
