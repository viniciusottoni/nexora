import { useEffect, useMemo, useState } from 'react';
import type { TableMapEntry } from '@nexora/contracts';
import { Button, Card, EmptyState, QuantityStepper, TableCard } from '@nexora/ui';
import type { OperationalRequestIdentity } from '@nexora/ui';
import { PosApiError, PosTablesApi } from './tables-api.js';
import './open-table.css';

// Ligado UMA VEZ no carregamento do módulo — nunca recriado inline no valor default de uma prop
// (isso geraria uma função NOVA a cada render, e como `fetcher` entra no array de deps do
// `useMemo` abaixo, cada render recriaria `api` e disparava `useEffect` de novo: loop infinito de
// requisição observado em teste real). Ver docstring de
// `packages/ui/src/auth/operational-authenticated-fetch.ts` sobre o motivo do `.bind`.
const boundFetch: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args);

export interface OpenTablePageProps {
  readonly identity: OperationalRequestIdentity;
  readonly baseUrl?: string;
  readonly fetcher?: typeof fetch;
  /** Mesa já escolhida no mapa (US-023) — pula a grade de escolha e vai direto à contagem de pessoas. */
  readonly preselectedTableId?: string;
  /** Chamado ao voltar quando se chegou aqui a partir do mapa — sem isto, "voltar" só limpa a seleção local. */
  readonly onExit?: () => void;
}

/**
 * Fluxo "abrir mesa" do garçom (US-022 §10: "abrir mesa em dois toques — escolher a mesa,
 * informar quantas pessoas"). A grade de escolha lê o mesmo mapa de mesas da US-023 filtrado a
 * livres; quando chega já com `preselectedTableId` (toque numa mesa livre no mapa), pula direto
 * para a contagem de pessoas.
 */
export function OpenTablePage({
  identity,
  baseUrl = '',
  fetcher = boundFetch,
  preselectedTableId,
  onExit,
}: Readonly<OpenTablePageProps>) {
  const api = useMemo(() => new PosTablesApi(identity, baseUrl, fetcher), [identity, baseUrl, fetcher]);
  const [tables, setTables] = useState<TableMapEntry[]>();
  const [loadError, setLoadError] = useState<string>();
  const [selected, setSelected] = useState<TableMapEntry | null>(null);
  const [guestCount, setGuestCount] = useState(2);
  const [busy, setBusy] = useState(false);
  const [openError, setOpenError] = useState<string>();
  // Feedback otimista (US-022 §10): a mesa some da lista de livres assim que o garçom confirma,
  // sem esperar a confirmação silenciosa do servidor.
  const [pendingIds, setPendingIds] = useState<ReadonlySet<string>>(new Set());

  useEffect(() => {
    let active = true;
    api
      .listFreeTables()
      .then((response) => {
        if (!active) return;
        setTables(response);
        // Veio do mapa (US-023) com uma mesa já escolhida — pula a grade e vai direto à contagem
        // de pessoas, sem exigir um segundo toque em cima do mesmo cartão.
        if (preselectedTableId) {
          const match = response.find((table) => table.id === preselectedTableId);
          if (match) setSelected(match);
        }
      })
      .catch(() => {
        if (active) setLoadError('Não foi possível carregar as mesas. Verifique a conexão local.');
      });
    return () => {
      active = false;
    };
  }, [api, preselectedTableId]);

  const freeTables = (tables ?? []).filter((table) => !pendingIds.has(table.id));

  async function confirmOpen() {
    if (!selected) return;
    const table = selected;
    setBusy(true);
    setOpenError(undefined);
    setPendingIds((prev) => new Set(prev).add(table.id));
    try {
      await api.openSession(table.id, { guestCount });
      setGuestCount(2);
      // Veio do mapa (US-023): volta pra lá já mostrando a mesa ocupada, em vez de uma grade de
      // livres que, na prática, só tinha essa única mesa pré-selecionada.
      if (onExit) {
        onExit();
      } else {
        setSelected(null);
      }
    } catch (cause) {
      setPendingIds((prev) => {
        const next = new Set(prev);
        next.delete(table.id);
        return next;
      });
      setOpenError(
        cause instanceof PosApiError && cause.code === 'TABLE_ALREADY_OPEN'
          ? 'Esta mesa já tem uma comanda em aberto.'
          : (cause instanceof Error ? cause.message : 'Não foi possível abrir a mesa.'),
      );
    } finally {
      setBusy(false);
    }
  }

  if (selected) {
    const maxGuests = Math.max(selected.seats * 3, 20);
    return (
      <Card as="section" className="pos-open-table-confirm">
        <p className="pos-eyebrow">Abrir mesa {selected.label}</p>
        <h2>Quantas pessoas sentaram?</h2>
        <QuantityStepper value={guestCount} min={1} max={maxGuests} onChange={setGuestCount} />
        {openError ? (
          <p role="alert" className="pos-open-table-error">
            {openError}
          </p>
        ) : null}
        <div className="pos-open-table-actions">
          <Button type="button" onClick={() => (onExit ? onExit() : setSelected(null))} disabled={busy}>
            Voltar
          </Button>
          <Button type="button" onClick={() => void confirmOpen()} disabled={busy}>
            {busy ? 'Abrindo…' : 'Abrir mesa'}
          </Button>
        </div>
      </Card>
    );
  }

  if (tables === undefined) {
    return (
      <p role="status" className="pos-loading">
        Carregando mesas…
      </p>
    );
  }

  if (loadError) {
    return <EmptyState icon="wifi_off" title="Não foi possível carregar as mesas">{loadError}</EmptyState>;
  }

  if (freeTables.length === 0) {
    return (
      <EmptyState icon="table_restaurant" title="Nenhuma mesa livre agora">
        Todas as mesas estão ocupadas no momento.
      </EmptyState>
    );
  }

  return (
    <section className="pos-open-table-list">
      <h2>Escolha a mesa</h2>
      <div className="pos-open-table-grid nx-stagger">
        {freeTables.map((table) => (
          <TableCard
            key={table.id}
            name={`Mesa ${table.label}`}
            status="FREE"
            guests={table.seats}
            onClick={() => setSelected(table)}
          />
        ))}
      </div>
    </section>
  );
}
