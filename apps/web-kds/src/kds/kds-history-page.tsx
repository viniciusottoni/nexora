import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Button, Icon, Input, type OperationalRequestIdentity } from '@nexora/ui';
import type { KdsHistoryItem } from '@nexora/contracts';
import { readStationIdFromAccessToken } from './decode-station-claim.js';
import { KdsHistoryApi, KdsHistoryApiError } from './kds-history-api.js';
import './kds-history-page.css';

export interface KdsHistoryPageProps {
  /** Vazio no edge (mesma origem) — parâmetro só para permitir apontar para outro host em teste. */
  readonly baseUrl?: string;
  readonly identity: Readonly<OperationalRequestIdentity>;
  /** US-046 §10 ("Saída do histórico de volta à fila em uma tecla") — Escape ou o botão "Voltar" disparam este callback; quem decide como encaixar a tela (overlay, toggle) é quem monta `KdsHistoryPage`. */
  readonly onClose: () => void;
}

/** Debounce da busca (US-046 §10, "busca por código curto como caminho principal") — evita um fetch por tecla digitada. */
const SEARCH_DEBOUNCE_MS = 300;

function formatClock(iso: string | null): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

function formatPrepDuration(seconds: number): string {
  const minutes = Math.floor(seconds / 60);
  const remainder = seconds % 60;
  return `${minutes}:${remainder.toString().padStart(2, '0')}`;
}

/**
 * US-046 (Histórico do turno no KDS) — painel separado da fila ativa: itens já SERVIDOS no dia
 * operacional corrente (ADR-018), do mais recente para o mais antigo, com busca por código curto do
 * pedido ou mesa e o resumo do turno (contagem + tempo médio de produção). Resolve a praça pela
 * mesma claim `stn` do token que `KdsQueuePage` usa — nunca escolhida na tela.
 *
 * Navegação 100% por teclado (US-046 §10): Escape fecha o painel e devolve o operador à fila sem
 * precisar de mouse, mesmo com o campo de busca focado.
 */
export function KdsHistoryPage({ baseUrl = '', identity, onClose }: Readonly<KdsHistoryPageProps>) {
  const stationId = useMemo(() => readStationIdFromAccessToken(identity.accessToken), [identity.accessToken]);
  const api = useMemo(() => new KdsHistoryApi(baseUrl), [baseUrl]);

  const [search, setSearch] = useState('');
  const [items, setItems] = useState<readonly KdsHistoryItem[] | null>(null);
  const [summary, setSummary] = useState<{ count: number; avgPrepSeconds: number }>();
  const [error, setError] = useState<string>();
  const [loading, setLoading] = useState(false);
  const searchInputRef = useRef<HTMLInputElement>(null);

  const load = useCallback(
    async (term: string) => {
      if (!stationId) return;
      setLoading(true);
      try {
        const response = await api.history(identity, stationId, term || undefined);
        setItems(response.items);
        setSummary(response.summary);
        setError(undefined);
      } catch (err) {
        setError(err instanceof KdsHistoryApiError ? err.message : 'Não foi possível carregar o histórico do turno.');
      } finally {
        setLoading(false);
      }
    },
    [api, identity, stationId],
  );

  useEffect(() => {
    const timeout = setTimeout(() => void load(search), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timeout);
  }, [load, search]);

  // Campo de busca sempre focado ao abrir — mesma convenção de foco automático de NumericKeypad.
  useEffect(() => {
    searchInputRef.current?.focus();
  }, []);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        onClose();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  if (!stationId) {
    return (
      <p className="kds-history__no-station nx-anim-in" role="status">
        Este terminal não está associado a nenhuma praça de produção.
      </p>
    );
  }

  return (
    <main className="kds-history nx-anim-in" data-surface="kds">
      <header className="kds-history__header">
        <div className="kds-history__heading">
          <h1>Histórico do turno</h1>
          <p className="kds-history__lead">Pedidos já concluídos no turno corrente.</p>
        </div>
        <Button type="button" variant="secondary" size="touch" onClick={onClose} data-testid="kds-history-close">
          <Icon name="arrow_back" size={20} /> Voltar
        </Button>
      </header>

      <div className="kds-history__toolbar">
        <Input
          ref={searchInputRef}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Buscar por código ou mesa"
          aria-label="Buscar por código do pedido ou mesa"
          icon={<Icon name="search" size={20} />}
          data-testid="kds-history-search"
        />
        {summary ? (
          <div className="kds-history__summary" data-testid="kds-history-summary">
            <span className="kds-history__summary-item">
              <strong>{summary.count}</strong> {summary.count === 1 ? 'pedido' : 'pedidos'}
            </span>
            <span className="kds-history__summary-item">
              tempo médio <strong>{formatPrepDuration(summary.avgPrepSeconds)}</strong>
            </span>
          </div>
        ) : null}
      </div>

      {error ? (
        <p className="kds-history__error nx-anim-in" role="alert">
          {error}
        </p>
      ) : null}

      {items === null && loading ? (
        <p className="kds-history__loading" role="status">
          Carregando histórico…
        </p>
      ) : items !== null && items.length === 0 ? (
        <p className="kds-history__empty" role="status">
          {search ? 'Nenhum item encontrado para esta busca.' : 'Nenhum item concluído neste turno ainda.'}
        </p>
      ) : items !== null ? (
        <ul className="kds-history__list nx-stagger">
          {items.map((item) => (
            <li key={item.orderItemId} className="kds-history__item" data-testid="kds-history-item">
              <div className="kds-history__item-main">
                <span className="kds-history__code">#{item.orderCode}</span>
                <span className="kds-history__product">{item.productName}</span>
                {item.table ? <span className="kds-history__table">Mesa {item.table}</span> : null}
              </div>
              <div className="kds-history__item-stamps">
                <span>disparo {formatClock(item.firedAt)}</span>
                <span>pronto {formatClock(item.readyAt)}</span>
                <span>prep. {formatPrepDuration(item.prepSeconds)}</span>
                {item.operator ? <span>por {item.operator.name}</span> : null}
              </div>
            </li>
          ))}
        </ul>
      ) : null}
    </main>
  );
}
