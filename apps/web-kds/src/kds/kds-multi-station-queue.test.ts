// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { KdsQueueItem } from '@nexora/contracts';
import { cleanup, renderHook, waitFor } from '@testing-library/react';
import { groupKdsQueueByStation, mergeKdsQueuesByStation, useMultiStationKdsQueue } from './kds-multi-station-queue.js';
import { KdsQueueApi } from './kds-queue-api.js';

const FORNO = '0198aabb-1111-7000-8000-000000000010';
const MONTAGEM = '0198aabb-1111-7000-8000-000000000020';

function item(id: string): KdsQueueItem {
  return {
    orderItemId: id,
    orderId: '0198aabb-5555-7000-8000-000000000001',
    orderCode: 'A1',
    productId: '0198aabb-6666-7000-8000-000000000001',
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

function jsonResponse(body: unknown): Response {
  return { ok: true, status: 200, json: () => Promise.resolve(body) } as unknown as Response;
}

/** `RequestInfo | URL` inclui `Request`, cuja stringificação padrão vira `"[object Object]"` — os
 * clientes deste app só chamam com `string`/`URL`, mas o tipo do parâmetro do `fetch` é mais largo. */
function requestUrl(input: RequestInfo | URL): string {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

describe('mergeKdsQueuesByStation / groupKdsQueueByStation (US-042 §3/§4)', () => {
  it('mescla os itens de cada praça marcando a origem, e o agrupamento preserva a ordem dos ids', () => {
    const byStation = new Map([
      [FORNO, [item('i1')]],
      [MONTAGEM, [item('i2'), item('i3')]],
    ]);

    const merged = mergeKdsQueuesByStation(byStation);
    expect(merged).toHaveLength(3);
    expect(merged.find((i) => i.orderItemId === 'i1')?.stationId).toBe(FORNO);
    expect(merged.find((i) => i.orderItemId === 'i2')?.stationId).toBe(MONTAGEM);

    const grouped = groupKdsQueueByStation(merged, [MONTAGEM, FORNO]);
    expect([...grouped.keys()]).toEqual([MONTAGEM, FORNO]);
    expect(grouped.get(MONTAGEM)).toHaveLength(2);
    expect(grouped.get(FORNO)).toHaveLength(1);
  });
});

describe('useMultiStationKdsQueue', () => {
  afterEach(() => {
    cleanup();
  });

  it('busca uma vez por praça (GET /v1/kds/queue?stationId=) e mescla os resultados', async () => {
    // `kdsQueueItemSchema.orderItemId`/`orderId` exigem UUID (packages/contracts/src/kds.ts) — o
    // parse do contrato rejeitaria 'i1'/'i2' soltos, então o item aqui usa UUIDs de verdade.
    const ITEM_1 = '0198aabb-3333-7000-8000-000000000001';
    const ITEM_2 = '0198aabb-3333-7000-8000-000000000002';
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url = requestUrl(input);
      if (url.includes(`stationId=${FORNO}`)) return jsonResponse({ items: [item(ITEM_1)], lastEventId: 'x' });
      if (url.includes(`stationId=${MONTAGEM}`)) return jsonResponse({ items: [item(ITEM_2)], lastEventId: 'x' });
      throw new Error(`unexpected: ${url}`);
    });
    const api = new KdsQueueApi('', fetcher);
    const identity = { accessToken: 't', deviceId: 'd', deviceSecret: 's' };

    const { result, unmount } = renderHook(() =>
      useMultiStationKdsQueue(identity, [FORNO, MONTAGEM], { api, pollMs: 0 }),
    );

    await waitFor(() => expect(result.current.items).not.toBeNull());
    expect(result.current.items?.map((i) => i.orderItemId).sort()).toEqual([ITEM_1, ITEM_2]);
    expect(result.current.items?.find((i) => i.orderItemId === ITEM_1)?.stationId).toBe(FORNO);
    expect(fetcher).toHaveBeenCalledTimes(2);
    unmount();
  });
});
