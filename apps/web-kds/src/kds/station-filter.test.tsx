// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { DevicePreferencesApi } from './device-preferences-api.js';
import { KdsQueueApi } from './kds-queue-api.js';
import { KdsStationsApi } from './stations-api.js';
import { StationFilterBar, useStationFilter, type UseStationFilterOptions } from './station-filter.js';

const FORNO = '0198aabb-1111-7000-8000-000000000010';
const MONTAGEM = '0198aabb-1111-7000-8000-000000000020';
const BEBIDAS = '0198aabb-1111-7000-8000-000000000030';

function base64Url(value: string): string {
  return Buffer.from(value, 'utf8').toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function makeAccessToken(stationId?: string): string {
  const header = base64Url(JSON.stringify({ alg: 'none' }));
  const payload = base64Url(JSON.stringify(stationId ? { stn: stationId } : {}));
  return `${header}.${payload}.sig`;
}

function identityFor(deviceId: string, stationId?: string) {
  return { accessToken: makeAccessToken(stationId), deviceId, deviceSecret: 'secret' };
}

function station(id: string, name: string, color: string, position: number) {
  return {
    id,
    code: name.toUpperCase(),
    name,
    color,
    capacitySlots: null,
    isBottleneck: false,
    position,
    isActive: true,
    linkedProductCount: 0,
  };
}

const ONE_STATION = [station(FORNO, 'Forno', 'red', 0)];
const THREE_STATIONS = [station(FORNO, 'Forno', 'red', 0), station(MONTAGEM, 'Montagem', 'blue', 1), station(BEBIDAS, 'Bebidas', 'teal', 2)];

/**
 * Node 22+ expõe um `localStorage` global NATIVO (atrás da flag `--localstorage-file`) que
 * SOMBREIA o `localStorage` do jsdom — `globalThis.localStorage` existe, mas `.clear()` etc. não
 * funcionam sem um arquivo de backing configurado (achado rodando esta suíte). Injetamos um storage
 * em memória só neste hook, para exercitar exatamente o cache que `station-filter.tsx` usa
 * (`readCachedSelection`/`writeCachedSelection`) sem depender desse detalhe de ambiente.
 */
function createMemoryStorage(): Storage {
  const store = new Map<string, string>();
  return {
    getItem: (key: string) => store.get(key) ?? null,
    setItem: (key: string, value: string) => void store.set(key, value),
    removeItem: (key: string) => void store.delete(key),
    clear: () => store.clear(),
    key: (index: number) => Array.from(store.keys())[index] ?? null,
    get length() {
      return store.size;
    },
  };
}

/** `kdsQueueItemSchema` exige forma completa (UUID em orderItemId/orderId etc.) — usado só onde a
 * contagem depende do parse real da resposta (`useStationFilter`'s "outras praças"). */
function queueItem(id: string) {
  return {
    orderItemId: id,
    orderId: `0198aabb-4444-7000-8000-000000000001`,
    orderCode: 'A1',
    productId: '0198aabb-4444-7000-8000-000000000099',
    productName: 'Pizza',
    quantity: 1,
    modifiers: [],
    notes: null,
    status: 'QUEUED',
    placedAt: '2026-08-04T12:00:00.000Z',
    elapsedSeconds: 0,
    thresholdState: 'NORMAL',
    warnSeconds: 300,
    criticalSeconds: 600,
    table: null,
    channel: 'DineIn',
    fractions: [],
  };
}

function jsonResponse(body: unknown, status = 200): Response {
  return { ok: status < 300, status, json: () => Promise.resolve(body) } as unknown as Response;
}

/** `RequestInfo | URL` inclui `Request`, cuja stringificação padrão vira `"[object Object]"` — os
 * clientes deste app só chamam com `string`/`URL`, mas o tipo do parâmetro do `fetch` é mais largo. */
function requestUrl(input: RequestInfo | URL): string {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

/** Fetcher único que resolve as três rotas usadas por este componente, igual ao padrão de `kds-queue-api.test.ts`. */
function makeFetcher(stations: readonly unknown[]) {
  const patchCalls: Array<{ url: string; body: unknown }> = [];
  const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = requestUrl(input);
    if (url.includes('/v1/catalog/stations')) {
      return jsonResponse({ items: stations });
    }
    if (url.includes('/v1/kds/queue')) {
      return jsonResponse({ items: [], lastEventId: '2026-08-04T12:00:00.000Z' });
    }
    if (init?.method === 'PATCH' && url.includes('/preferences')) {
      // O corpo enviado por `DevicePreferencesApi` é sempre `JSON.stringify(...)` (string) — ver
      // `device-preferences-api.ts`; o tipo largo de `BodyInit` é do `fetch`, não do chamador real.
      const body = JSON.parse(init.body as string) as unknown;
      patchCalls.push({ url, body });
      return jsonResponse({ deviceId: 'device-1', preferences: { kds: (body as { preferences: { kds: unknown } }).preferences.kds } });
    }
    throw new Error(`unexpected request: ${url}`);
  });
  return { fetcher, patchCalls };
}

function renderFilter(
  overrides: Partial<UseStationFilterOptions> & { identity: UseStationFilterOptions['identity'] },
  fetcher: typeof fetch,
) {
  const stationsApi = overrides.stationsApi ?? new KdsStationsApi('', fetcher);
  const preferencesApi = overrides.preferencesApi ?? new DevicePreferencesApi('', fetcher);
  const queueApi = overrides.queueApi ?? new KdsQueueApi('', fetcher);
  const storage = overrides.storage ?? createMemoryStorage();

  function Harness() {
    const filter = useStationFilter({ ...overrides, stationsApi, preferencesApi, queueApi, storage });
    return <StationFilterBar filter={filter} />;
  }

  return { ...render(<Harness />), storage };
}

describe('useStationFilter / StationFilterBar (US-042)', () => {
  afterEach(() => {
    cleanup();
  });

  it('praça única no tenant: filtro fica oculto (Cenário "Cozinha pequena com praça única")', async () => {
    const { fetcher } = makeFetcher(ONE_STATION);
    const identity = identityFor('device-single', FORNO);
    const { container } = renderFilter({ identity }, fetcher);

    await waitFor(() => expect(fetcher).toHaveBeenCalled());
    await waitFor(() => expect(container).toBeEmptyDOMElement());
  });

  it('múltiplas praças sem preferência salva: usa a praça da claim do token como default', async () => {
    const { fetcher } = makeFetcher(THREE_STATIONS);
    const identity = identityFor('device-multi', MONTAGEM);
    renderFilter({ identity }, fetcher);

    await waitFor(() => expect(screen.getByTestId('kds-station-filter')).toBeInTheDocument());
    expect(screen.getByTestId('kds-station-filter-toggle')).toHaveTextContent('Montagem');
  });

  it('persistência por dispositivo: preferência já salva no navegador sobrevive a um novo carregamento (US-042 §4 "reiniciado")', async () => {
    const storage = createMemoryStorage();
    storage.setItem('nexora:kds:station-filter:device-restart', JSON.stringify([BEBIDAS]));
    const { fetcher } = makeFetcher(THREE_STATIONS);
    const identity = identityFor('device-restart', FORNO);
    renderFilter({ identity, storage }, fetcher);

    await waitFor(() => expect(screen.getByTestId('kds-station-filter-toggle')).toHaveTextContent('Bebidas'));
  });

  it('trocar de praça exige confirmação antes de aplicar — cancelar não muda nada', async () => {
    const { fetcher, patchCalls } = makeFetcher(THREE_STATIONS);
    const identity = identityFor('device-cancel', FORNO);
    renderFilter({ identity }, fetcher);

    await waitFor(() => expect(screen.getByTestId('kds-station-filter-toggle')).toHaveTextContent('Forno'));

    fireEvent.click(screen.getByTestId('kds-station-filter-toggle'));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Montagem' }));
    fireEvent.click(screen.getByTestId('kds-station-filter-apply'));

    expect(await screen.findByText('Confirmar troca de praça')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }));

    await waitFor(() => expect(screen.queryByText('Confirmar troca de praça')).not.toBeInTheDocument());
    expect(screen.getByTestId('kds-station-filter-toggle')).toHaveTextContent('Forno');
    expect(patchCalls).toHaveLength(0);
  });

  it('confirmar a troca persiste no dispositivo com o corpo {"preferences":{"kds":{"stationIds":[...]}}} (ADR-020 Idempotency-Key incluso)', async () => {
    const { fetcher, patchCalls } = makeFetcher(THREE_STATIONS);
    const identity = identityFor('device-confirm', FORNO);
    const { storage } = renderFilter({ identity }, fetcher);

    await waitFor(() => expect(screen.getByTestId('kds-station-filter-toggle')).toHaveTextContent('Forno'));

    fireEvent.click(screen.getByTestId('kds-station-filter-toggle'));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Montagem' }));
    fireEvent.click(screen.getByTestId('kds-station-filter-apply'));
    fireEvent.click(await screen.findByTestId('kds-station-filter-confirm'));

    await waitFor(() => expect(screen.getByTestId('kds-station-filter-toggle')).toHaveTextContent('Forno + Montagem'));

    expect(patchCalls).toHaveLength(1);
    expect(patchCalls[0]?.body).toMatchObject({ preferences: { kds: { stationIds: [FORNO, MONTAGEM] } } });

    const patchCall = fetcher.mock.calls.find((call) => call[1]?.method === 'PATCH');
    expect(new Headers(patchCall?.[1]?.headers).get('Idempotency-Key')).toBeTruthy();

    expect(JSON.parse(storage.getItem('nexora:kds:station-filter:device-confirm') ?? '[]')).toEqual([FORNO, MONTAGEM]);
  });

  it('modo "Todas as praças" (supervisão) mostra o rótulo de supervisão e nenhuma praça de fora', async () => {
    const { fetcher } = makeFetcher(THREE_STATIONS);
    const identity = identityFor('device-all', FORNO);
    renderFilter({ identity }, fetcher);

    await waitFor(() => expect(screen.getByTestId('kds-station-filter-toggle')).toHaveTextContent('Forno'));

    fireEvent.click(screen.getByTestId('kds-station-filter-toggle'));
    fireEvent.click(screen.getByRole('button', { name: 'Todas as praças' }));
    fireEvent.click(await screen.findByTestId('kds-station-filter-confirm'));

    await waitFor(() => expect(screen.getByTestId('kds-station-filter-toggle')).toHaveTextContent('Todas as praças'));
    expect(screen.queryByTestId('kds-station-filter-others')).not.toBeInTheDocument();
  });

  it('contagem discreta das outras praças aparece quando o filtro está restrito a uma praça', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url = requestUrl(input);
      if (url.includes('/v1/catalog/stations')) return jsonResponse({ items: THREE_STATIONS });
      if (url.includes(`stationId=${MONTAGEM}`)) {
        return jsonResponse({
          items: [
            queueItem('0198aabb-4444-7000-8000-000000000010'),
            queueItem('0198aabb-4444-7000-8000-000000000011'),
          ],
          lastEventId: 'x',
        });
      }
      if (url.includes('/v1/kds/queue')) return jsonResponse({ items: [], lastEventId: 'x' });
      throw new Error(`unexpected: ${url}`);
    });
    const identity = identityFor('device-others', FORNO);
    renderFilter({ identity }, fetcher);

    await waitFor(() => expect(screen.getByTestId('kds-station-filter-others')).toBeInTheDocument());
    await waitFor(() => expect(screen.getByTestId('kds-station-filter-others')).toHaveTextContent('Montagem · 2'));
    expect(screen.getByTestId('kds-station-filter-others')).toHaveTextContent('Bebidas · 0');
  });

  it('quando a lista de praças falha (offline/rota indisponível), degrada para o modo praça única sem travar a tela', async () => {
    const fetcher = vi.fn(async () => jsonResponse({ code: 'BOOM', detail: 'falhou' }, 500));
    const identity = identityFor('device-error', FORNO);
    const { container } = renderFilter({ identity }, fetcher);

    await waitFor(() => expect(container).toBeEmptyDOMElement());
  });
});
