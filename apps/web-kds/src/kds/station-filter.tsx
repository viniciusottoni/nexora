import { useCallback, useEffect, useState } from 'react';
import type { StationDto } from '@nexora/contracts';
import { Button, Icon, Modal, type OperationalRequestIdentity } from '@nexora/ui';
import { readStationIdFromAccessToken } from './decode-station-claim.js';
import { DevicePreferencesApi } from './device-preferences-api.js';
import { KdsQueueApi } from './kds-queue-api.js';
import { KdsStationsApi, stationColorCssValue } from './stations-api.js';
import './station-filter.css';

/**
 * US-042 — três modos possíveis, resolvidos a partir de quantas praças o tenant tem cadastradas e
 * de qual seleção o operador (ou o default herdado da claim `stn`) escolheu:
 * - `single`  — tenant só tem uma praça ativa: filtro fica OCULTO, comportamento idêntico ao que
 *               já existia antes desta história (Cenário "Cozinha pequena com praça única").
 * - `filtered`— seleção não vazia (uma ou mais praças): a fila mostra só os itens dessas praças.
 * - `all`     — seleção vazia por escolha explícita ("Todas as praças"): modo supervisão.
 */
export type StationFilterMode = 'single' | 'filtered' | 'all';

/** Intervalo de atualização da contagem discreta das OUTRAS praças — mais espaçado que o poll
 * principal da fila (5s, `kds-queue-page.tsx`/ADR-011) porque é informação secundária, não a fila
 * que a praça ativa está de fato trabalhando. */
const OTHER_STATIONS_POLL_MS = 20_000;

const NONE_SELECTED: readonly string[] = [];
const EMPTY_STATIONS: readonly StationDto[] = [];

function storageKey(deviceId: string): string {
  return `nexora:kds:station-filter:${deviceId}`;
}

/** Cache no navegador (US-042 §9: "preferência do dispositivo guardada no edge e em cache no
 * navegador") — é a fonte de leitura PRINCIPAL na inicialização: não existe `GET` de preferência
 * isolada no backend (só o `PATCH` devolve o estado mesclado, ver docstring de
 * `DevicePreferencesApi`), então o cache local é o que garante "abre de novo na praça Forno, sem
 * reconfiguração" no cenário de reinício do dispositivo. */
type StationFilterStorage = Pick<Storage, 'getItem' | 'setItem'>;

function defaultStorage(): StationFilterStorage | undefined {
  return typeof globalThis.localStorage === 'undefined' ? undefined : globalThis.localStorage;
}

function readCachedSelection(
  deviceId: string | undefined,
  storage: StationFilterStorage | undefined,
): readonly string[] | undefined {
  if (!deviceId) return undefined;
  try {
    const raw = storage?.getItem(storageKey(deviceId));
    if (raw == null) return undefined;
    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) return undefined;
    return parsed.filter((value): value is string => typeof value === 'string');
  } catch {
    // Cache é só conveniência (mesmo raciocínio de sound-preferences.tsx) — corrompido ou
    // indisponível (modo privado, quota) volta ao default sem quebrar a tela.
    return undefined;
  }
}

function writeCachedSelection(
  deviceId: string | undefined,
  selection: readonly string[],
  storage: StationFilterStorage | undefined,
): void {
  if (!deviceId) return;
  try {
    storage?.setItem(storageKey(deviceId), JSON.stringify(selection));
  } catch {
    // Ver readCachedSelection.
  }
}

export interface UseStationFilterOptions {
  readonly identity: Readonly<OperationalRequestIdentity>;
  /** Vazio no edge (mesma origem) — mesmo parâmetro/motivo de `KdsQueuePageProps.baseUrl`. */
  readonly baseUrl?: string;
  /** Injeção para teste — mesmo padrão de `KdsQueueApi`/`DevicePreferencesApi` aceitarem `fetcher`. */
  readonly stationsApi?: KdsStationsApi;
  readonly preferencesApi?: DevicePreferencesApi;
  readonly queueApi?: KdsQueueApi;
  readonly storage?: StationFilterStorage;
}

export interface UseStationFilterResult {
  readonly loading: boolean;
  readonly error: string | undefined;
  /** Todas as praças ATIVAS do tenant (já sem soft-deleted/inativas) — fonte do seletor. */
  readonly stations: readonly StationDto[];
  readonly mode: StationFilterMode;
  /**
   * IDs efetivos a consultar na fila — uma chamada de `GET /v1/kds/queue?stationId=` POR id desta
   * lista, mesclada client-side (US-042 §7; `kds-multi-station-queue.ts` faz exatamente isso).
   * `Nexora.Api.Edge/Controllers/KdsController.cs` exige `stationId` como `Guid` não-nulo
   * (confirmado no controller) — não existe "buscar sem stationId" no backend, então em
   * `mode==='all'` esta lista contém TODOS os ids de `stations`, nunca vazia. Só fica vazia em
   * `mode==='single'`, onde o comportamento continua sendo o legado da claim do token, fora do
   * escopo deste hook.
   */
  readonly selectedStationIds: readonly string[];
  /** Praças exibidas AGORA (para nome/cor no cabeçalho) — todas quando `mode==='all'`. */
  readonly activeStations: readonly StationDto[];
  /** Praças de FORA do filtro atual — só populado em `mode==='filtered'` (US-042 §10, "contagem
   * discreta de itens pendentes nas outras praças"). */
  readonly otherStations: readonly StationDto[];
  /** `stationId → contagem de itens ativos`, atualizado a cada `OTHER_STATIONS_POLL_MS` só para as
   * praças de `otherStations`. Ausência de uma chave = ainda não carregou. */
  readonly otherStationsPendingCounts: Readonly<Record<string, number>>;
  /** Seleção aguardando confirmação (US-042 §10 "troca de praça protegida por confirmação") —
   * `undefined` quando não há troca pendente. */
  readonly pendingSelection: readonly string[] | undefined;
  readonly saving: boolean;
  /** Abre a confirmação para a próxima seleção (`[]` = "Todas as praças"). */
  readonly requestChange: (next: readonly string[]) => void;
  /** Aplica a seleção pendente: grava no cache local (otimista) e tenta persistir no dispositivo. */
  readonly confirmChange: () => Promise<void>;
  readonly cancelChange: () => void;
}

/**
 * Hook central da US-042 — carrega as praças do tenant, resolve/persiste a seleção do filtro POR
 * DISPOSITIVO e mantém a contagem discreta de itens pendentes nas praças não selecionadas.
 * Autossuficiente: não depende de nenhum estado de `kds-queue-page.tsx` (arquivo intocável nesta
 * história) — quem integrar decide como usar `selectedStationIds`/`mode` para buscar a fila (ver
 * `kds-multi-station-queue.ts` para o utilitário de mesclagem multi-praça).
 */
export function useStationFilter(options: Readonly<UseStationFilterOptions>): UseStationFilterResult {
  const { identity, baseUrl = '' } = options;
  const [stationsApi] = useState(() => options.stationsApi ?? new KdsStationsApi(baseUrl));
  const [preferencesApi] = useState(() => options.preferencesApi ?? new DevicePreferencesApi(baseUrl));
  const [queueApi] = useState(() => options.queueApi ?? new KdsQueueApi(baseUrl));
  const [storage] = useState(() => options.storage ?? defaultStorage());

  const [stations, setStations] = useState<readonly StationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();
  const [selection, setSelection] = useState<readonly string[]>(NONE_SELECTED);
  const [pendingSelection, setPendingSelection] = useState<readonly string[]>();
  const [saving, setSaving] = useState(false);
  const [otherStationsPendingCounts, setOtherStationsPendingCounts] = useState<Readonly<Record<string, number>>>({});

  const deviceId = identity.deviceId;
  const accessToken = identity.accessToken;

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    stationsApi
      .list(identity)
      .then((response) => {
        if (cancelled) return;
        const active = response.items.filter((station) => station.isActive);
        setStations(active);
        setError(undefined);

        if (active.length > 1) {
          const cached = readCachedSelection(deviceId, storage);
          if (cached) {
            setSelection(cached.filter((id) => active.some((station) => station.id === id)));
          } else {
            const claimStationId = readStationIdFromAccessToken(accessToken);
            const fallback =
              claimStationId && active.some((station) => station.id === claimStationId) ? [claimStationId] : NONE_SELECTED;
            setSelection(fallback);
            writeCachedSelection(deviceId, fallback, storage);
          }
        }
      })
      .catch(() => {
        if (cancelled) return;
        // Degrada para modo praça única (filtro oculto) — ver docstring de KdsStationsApi sobre a
        // rota hoje só existir na nuvem. Nunca trava a tela do KDS por isso (RNF de disponibilidade).
        setStations([]);
        setError('Não foi possível carregar as praças de produção — mostrando a praça deste terminal.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [stationsApi, identity, deviceId, accessToken, storage]);

  // Contagem discreta das praças NÃO selecionadas (US-042 §10) — só roda em modo 'filtered'.
  useEffect(() => {
    if (stations.length <= 1 || selection.length === 0) {
      setOtherStationsPendingCounts({});
      return;
    }
    const others = stations.filter((station) => !selection.includes(station.id));
    if (others.length === 0) {
      setOtherStationsPendingCounts({});
      return;
    }

    let cancelled = false;
    const fetchCounts = () => {
      void Promise.all(
        others.map(async (station) => {
          try {
            const response = await queueApi.queue(identity, station.id);
            return [station.id, response.items.length] as const;
          } catch {
            return [station.id, undefined] as const;
          }
        }),
      ).then((entries) => {
        if (cancelled) return;
        setOtherStationsPendingCounts((prev) => {
          const next = { ...prev };
          for (const [id, count] of entries) {
            if (count !== undefined) next[id] = count;
          }
          return next;
        });
      });
    };

    fetchCounts();
    const interval = setInterval(fetchCounts, OTHER_STATIONS_POLL_MS);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, [stations, selection, queueApi, identity]);

  const requestChange = useCallback((next: readonly string[]) => {
    setPendingSelection(next);
  }, []);

  const cancelChange = useCallback(() => {
    setPendingSelection(undefined);
  }, []);

  const confirmChange = useCallback(async () => {
    if (pendingSelection === undefined) return;
    const next = pendingSelection;
    // Otimista: a troca já vale neste terminal (cache local) antes mesmo da resposta do PATCH —
    // é o "local-first" do US-042 §9, não o resultado da chamada ao servidor.
    setSelection(next);
    writeCachedSelection(deviceId, next, storage);
    setPendingSelection(undefined);

    setSaving(true);
    try {
      await preferencesApi.updateKdsPreferences(identity, { stationIds: [...next] });
      setError(undefined);
    } catch (err) {
      // A troca já vale neste terminal mesmo se a gravação no servidor falhar — só avisa, nunca
      // desfaz a escolha do operador.
      setError(
        err instanceof Error ? err.message : 'Não foi possível salvar a praça no servidor — aplicada só neste terminal.',
      );
    } finally {
      setSaving(false);
    }
  }, [pendingSelection, deviceId, identity, preferencesApi, storage]);

  let mode: StationFilterMode;
  if (stations.length <= 1) {
    mode = 'single';
  } else if (selection.length === 0) {
    mode = 'all';
  } else {
    mode = 'filtered';
  }

  let selectedStationIds: readonly string[] = NONE_SELECTED;
  if (mode === 'filtered') {
    selectedStationIds = selection;
  } else if (mode === 'all') {
    // GetKdsQueueQuery exige stationId (Guid não-nulo) — supervisão itera TODAS as praças.
    selectedStationIds = stations.map((station) => station.id);
  }

  let activeStations: readonly StationDto[] = EMPTY_STATIONS;
  if (mode === 'all') {
    activeStations = stations;
  } else if (mode === 'filtered') {
    activeStations = stations.filter((station) => selection.includes(station.id));
  }

  const otherStations: readonly StationDto[] =
    mode === 'filtered' ? stations.filter((station) => !selection.includes(station.id)) : EMPTY_STATIONS;

  return {
    loading,
    error,
    stations,
    mode,
    selectedStationIds,
    activeStations,
    otherStations,
    otherStationsPendingCounts,
    pendingSelection,
    saving,
    requestChange,
    confirmChange,
    cancelChange,
  };
}

const ALL_STATIONS_LABEL = 'Todas as praças';

function describeSelection(stations: readonly StationDto[], ids: readonly string[]): string {
  if (ids.length === 0) return ALL_STATIONS_LABEL;
  const names = stations.filter((station) => ids.includes(station.id)).map((station) => station.name);
  return names.length > 0 ? names.join(' + ') : ALL_STATIONS_LABEL;
}

export interface StationFilterBarProps {
  readonly filter: UseStationFilterResult;
}

/**
 * Barra de filtro do KDS (US-042 §10) — praça ativa em destaque, seletor com confirmação e
 * contagem discreta das outras praças. Fica totalmente OCULTA quando `filter.mode === 'single'`
 * (Cenário "Cozinha pequena com praça única": "o filtro não deve ser exibido").
 */
export function StationFilterBar({ filter }: Readonly<StationFilterBarProps>) {
  const [pickerOpen, setPickerOpen] = useState(false);
  // Sentinel de SELEÇÃO (não de consulta): `[]` = "Todas as praças" está marcado no seletor.
  // Diferente de `filter.selectedStationIds`, que em `mode==='all'` já vem resolvido com TODOS os
  // ids (é o que a busca da fila precisa) — usar ele aqui marcaria toda praça como escolhida
  // individualmente em vez de mostrar "Todas as praças" selecionado.
  const currentSelection = filter.mode === 'filtered' ? filter.selectedStationIds : [];
  const currentSelectionKey = currentSelection.join(',');
  const [draft, setDraft] = useState<readonly string[]>(currentSelection);

  useEffect(() => {
    if (!pickerOpen) setDraft(currentSelection);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- currentSelectionKey representa currentSelection de forma estável
  }, [pickerOpen, currentSelectionKey]);

  if (filter.loading || filter.mode === 'single') return null;

  function toggleStation(id: string) {
    setDraft((prev) => (prev.includes(id) ? prev.filter((existing) => existing !== id) : [...prev, id]));
  }

  function applyDraft() {
    setPickerOpen(false);
    filter.requestChange(draft);
  }

  function selectAllStations() {
    setPickerOpen(false);
    filter.requestChange([]);
  }

  const pendingLabel =
    filter.pendingSelection !== undefined ? describeSelection(filter.stations, filter.pendingSelection) : undefined;

  return (
    <div className="kds-station-filter nx-anim-in" data-testid="kds-station-filter">
      <button
        type="button"
        className="kds-station-filter__summary"
        onClick={() => setPickerOpen((open) => !open)}
        aria-expanded={pickerOpen}
        aria-haspopup="true"
        data-testid="kds-station-filter-toggle"
      >
        <span className="kds-station-filter__dots" aria-hidden="true">
          {filter.mode === 'all' ? (
            <Icon name="grid_view" size={22} />
          ) : (
            filter.activeStations.map((station) => (
              <span
                key={station.id}
                className="kds-station-filter__dot"
                style={{ background: stationColorCssValue(station.color) }}
              />
            ))
          )}
        </span>
        <span className="kds-station-filter__label">
          {filter.mode === 'all' ? ALL_STATIONS_LABEL : describeSelection(filter.stations, currentSelection)}
        </span>
        <Icon name={pickerOpen ? 'expand_less' : 'expand_more'} size={20} />
      </button>

      {filter.otherStations.length > 0 ? (
        <p className="kds-station-filter__others" data-testid="kds-station-filter-others">
          {filter.otherStations
            .map((station) => `${station.name} · ${filter.otherStationsPendingCounts[station.id] ?? '—'}`)
            .join('  ')}
        </p>
      ) : null}

      {pickerOpen ? (
        <div className="kds-station-filter__picker nx-anim-in" aria-label="Escolher praça de produção">
          <button type="button" className="kds-station-filter__option" onClick={selectAllStations}>
            <Icon name={draft.length === 0 ? 'radio_button_checked' : 'radio_button_unchecked'} size={20} />
            {ALL_STATIONS_LABEL}
          </button>
          {filter.stations.map((station) => (
            <label key={station.id} className="kds-station-filter__option">
              <input
                type="checkbox"
                checked={draft.includes(station.id)}
                onChange={() => toggleStation(station.id)}
              />
              <span className="kds-station-filter__dot" style={{ background: stationColorCssValue(station.color) }} />
              {station.name}
            </label>
          ))}
          <Button type="button" size="touch" block onClick={applyDraft} data-testid="kds-station-filter-apply">
            Aplicar
          </Button>
        </div>
      ) : null}

      <Modal
        open={filter.pendingSelection !== undefined}
        onClose={filter.cancelChange}
        eyebrow="Confirmar troca de praça"
        title="Trocar a praça deste terminal?"
        tone="danger"
        actions={
          <>
            <Button type="button" variant="secondary" size="touch" onClick={filter.cancelChange}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant="danger"
              size="touch"
              busy={filter.saving}
              onClick={() => void filter.confirmChange()}
              data-testid="kds-station-filter-confirm"
            >
              Confirmar
            </Button>
          </>
        }
      >
        <p>
          A fila vai passar a mostrar <strong>{pendingLabel}</strong>. Evite trocar no meio do pico — a
          equipe pode perder pedido de vista por alguns segundos.
        </p>
      </Modal>

      {filter.error ? (
        <p className="kds-station-filter__error nx-anim-in" role="alert">
          {filter.error}
        </p>
      ) : null}
    </div>
  );
}
