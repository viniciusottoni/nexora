import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { SessionConsumptionItemDto, SessionConsumptionResponse, TableConsumptionEvent } from '@nexora/contracts';
import { EmptyState, Icon, OrderLine, StatusPill, SyncStatus, type StatusPillStatus, type SyncStatusState } from '@nexora/ui';
import { ConsumptionApi, ConsumptionApiError, ConsumptionRealtimeConnection, type ConsumptionMode } from './consumption-api.js';
import './consumption-view.css';

// Ligado UMA VEZ no carregamento do módulo — nunca recriado inline no valor default de uma prop
// (isso geraria uma função NOVA a cada render, e como `fetcher` entra no array de deps do
// `useMemo` abaixo, cada render recriaria `api` e disparava `useEffect` de novo: loop infinito de
// requisição observado em teste real). Ver docstring de
// `packages/ui/src/auth/operational-authenticated-fetch.ts` sobre o motivo do `.bind`.
const boundFetch: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args);

export interface ConsumptionViewProps {
  readonly sessionToken: string;
  readonly baseUrl?: string;
  readonly fetcher?: typeof fetch;
  /** Intervalo de polling (ms) — só para encurtar em teste; produção usa o default de 5000ms (ADR-011). */
  readonly pollIntervalMs?: number;
}

interface RepeatFeedback {
  readonly kind: 'success' | 'error';
  readonly message: string;
}

/**
 * Aba "Consumo" da tela do cliente (US-024) — lista dos itens já lançados na sessão com
 * quantidade/valor/status traduzido, subtotal/taxa de serviço/total (US-024 §10: total sempre
 * visível, fixo no rodapé; taxa sempre destacada como opcional) e ação de repetir com um toque
 * (US-028). Atualização em tempo real via SignalR com fallback de polling a cada 5s (ADR-011),
 * sinalizado pelo indicador `SyncStatus`.
 */
export function ConsumptionView({
  sessionToken,
  baseUrl = '',
  fetcher = boundFetch,
  pollIntervalMs = 5000,
}: Readonly<ConsumptionViewProps>) {
  const api = useMemo(() => new ConsumptionApi(sessionToken, baseUrl, fetcher), [sessionToken, baseUrl, fetcher]);
  const [consumption, setConsumption] = useState<SessionConsumptionResponse | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [syncMode, setSyncMode] = useState<ConsumptionMode>('ws');
  const [repeatingId, setRepeatingId] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<RepeatFeedback | null>(null);
  const connectionRef = useRef<ConsumptionRealtimeConnection | null>(null);

  const reload = useCallback(async () => {
    try {
      const data = await api.getCurrentConsumption();
      setConsumption(data);
      setLoadError(null);
    } catch (cause) {
      setLoadError(cause instanceof ConsumptionApiError ? cause.message : 'Não foi possível carregar o consumo agora.');
    }
  }, [api]);

  useEffect(() => {
    let active = true;
    void reload();

    function handleEvent(_event: TableConsumptionEvent) {
      // Qualquer evento de consumo (item lançado, status mudou) — recarrega a lista inteira.
      // Simplicidade deliberada: a lista raramente passa de poucos itens por mesa, e isso evita
      // reimplementar reconciliação incremental por item nesta wave.
      if (active) void reload();
    }

    const connection = new ConsumptionRealtimeConnection(
      sessionToken,
      api,
      { onEvent: handleEvent, onModeChange: setSyncMode },
      baseUrl,
      pollIntervalMs,
    );
    connectionRef.current = connection;
    void connection.start();

    return () => {
      active = false;
      void connection.stop();
    };
  }, [api, sessionToken, baseUrl, pollIntervalMs, reload]);

  const handleRepeat = useCallback(
    async (item: SessionConsumptionItemDto) => {
      setRepeatingId(item.orderItemId);
      setFeedback(null);
      try {
        const result = await api.repeatItem(item.orderId, item.orderItemId);
        const priceChanged = result.unitPrice !== item.unitPrice;
        setFeedback({
          kind: 'success',
          message: priceChanged
            ? `Item repetido — preço atual R$ ${formatMoney(result.unitPrice)} (era R$ ${formatMoney(item.unitPrice)})`
            : `Item repetido — R$ ${formatMoney(result.unitPrice)}`,
        });
        await reload();
      } catch (cause) {
        setFeedback({
          kind: 'error',
          message:
            cause instanceof ConsumptionApiError && cause.code === 'PRODUCT_UNAVAILABLE'
              ? 'Não é possível repetir — este item está indisponível no momento.'
              : 'Não foi possível repetir o item agora.',
        });
      } finally {
        setRepeatingId(null);
      }
    },
    [api, reload],
  );

  if (loadError && !consumption) {
    return (
      <div className="consumption-view">
        <EmptyState icon="receipt_long" title="Não foi possível carregar o consumo">
          {loadError}
        </EmptyState>
      </div>
    );
  }

  if (!consumption) {
    return (
      <output className="consumption-view" aria-label="Carregando consumo">
        <EmptyState icon="receipt_long" title="Carregando consumo…">
          Só um instante.
        </EmptyState>
      </output>
    );
  }

  return (
    <section className="consumption-view" aria-label="Consumo da mesa">
      <header className="consumption-view__head">
        <h2>Consumo</h2>
        <SyncStatus state={toSyncStatusState(syncMode)} />
      </header>

      {feedback ? (
        <output className={`consumption-view__feedback consumption-view__feedback--${feedback.kind}`}>
          {feedback.message}
        </output>
      ) : null}

      {consumption.items.length === 0 ? (
        <EmptyState icon="restaurant">Nenhum item lançado ainda — peça ao garçom ou pelo cardápio.</EmptyState>
      ) : (
        <ul className="consumption-view__items nx-stagger">
          {consumption.items.map((item) => (
            <li key={item.orderItemId}>
              <OrderLine
                qty={item.quantity}
                name={item.name}
                cancelled={item.cancelled}
                status={<StatusPill status={item.status as StatusPillStatus} label={item.statusLabel} size="md" />}
                price={`R$ ${formatMoney(item.total)}`}
                actions={
                  !item.cancelled ? (
                    <button
                      type="button"
                      className="consumption-view__repeat"
                      disabled={repeatingId === item.orderItemId || !item.productAvailable}
                      onClick={() => void handleRepeat(item)}
                      title={item.productAvailable ? 'Repetir este item' : 'Item indisponível no momento'}
                    >
                      <Icon name="replay" size={18} />
                      Repetir
                    </button>
                  ) : null
                }
              />
            </li>
          ))}
        </ul>
      )}

      <footer className="consumption-view__totals">
        <div className="consumption-view__totals-row">
          <span>Subtotal</span>
          <span key={consumption.subtotal} className="consumption-view__totals-value nx-anim-flash">
            R$ {formatMoney(consumption.subtotal)}
          </span>
        </div>
        <div className="consumption-view__totals-row consumption-view__totals-row--fee">
          <span>
            {'Taxa de serviço '}
            <span className="consumption-view__fee-badge">opcional</span>
          </span>
          <span key={consumption.serviceFee} className="consumption-view__totals-value nx-anim-flash">
            R$ {formatMoney(consumption.serviceFee)}
          </span>
        </div>
        <div className="consumption-view__totals-row consumption-view__totals-row--total">
          <span>Total</span>
          <span key={consumption.total} className="consumption-view__totals-value nx-anim-flash">
            R$ {formatMoney(consumption.total)}
          </span>
        </div>
      </footer>
    </section>
  );
}

function toSyncStatusState(mode: ConsumptionMode): SyncStatusState {
  return mode === 'ws' ? 'online' : 'delayed';
}

function formatMoney(value: string): string {
  const parsed = Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed.toFixed(2).replace('.', ',') : value;
}
