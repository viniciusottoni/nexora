import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { KdsQueueItem } from '@nexora/contracts';
import type { OperationalRequestIdentity } from '@nexora/ui';
import { KdsQueueApi } from './kds-queue-api.js';

/** Item da fila com a praça de origem anexada — `kdsQueueItemSchema` (packages/contracts/src/kds.ts)
 * não tem `stationId` (o item já chega filtrado por uma única praça em cada resposta), então quem
 * mescla várias respostas precisa marcar a origem antes de misturar os arrays. */
export interface KdsQueueItemWithStation extends KdsQueueItem {
  readonly stationId: string;
}

/** Junta o resultado de N chamadas `GET /v1/kds/queue?stationId=` (uma por praça) num único array,
 * cada item marcado com a praça de onde veio — base de US-042 §3 ("múltiplas praças na mesma
 * tela") e §4 ("visão de supervisão... agrupada por praça"). */
export function mergeKdsQueuesByStation(
  byStation: ReadonlyMap<string, readonly KdsQueueItem[]>,
): readonly KdsQueueItemWithStation[] {
  const merged: KdsQueueItemWithStation[] = [];
  for (const [stationId, items] of byStation) {
    for (const item of items) merged.push({ ...item, stationId });
  }
  return merged;
}

/** Agrupa por `stationId`, preservando a ordem de chegada de `stationIds` — usado para renderizar
 * a fila "agrupada por praça" do modo supervisão (US-042 §4/Cenário "Visão de supervisão"). */
export function groupKdsQueueByStation(
  items: readonly KdsQueueItemWithStation[],
  stationIds: readonly string[],
): ReadonlyMap<string, readonly KdsQueueItemWithStation[]> {
  const groups = new Map<string, KdsQueueItemWithStation[]>();
  for (const stationId of stationIds) groups.set(stationId, []);
  for (const item of items) {
    const bucket = groups.get(item.stationId);
    if (bucket) {
      bucket.push(item);
    } else {
      groups.set(item.stationId, [item]);
    }
  }
  return groups;
}

export interface UseMultiStationKdsQueueResult {
  /** `null` enquanto a primeira carga não terminou — mesma convenção de `kds-queue-page.tsx` (`items === null` → "Carregando fila…"). */
  readonly items: readonly KdsQueueItemWithStation[] | null;
  readonly error: string | undefined;
  readonly refresh: () => Promise<void>;
}

/**
 * US-042 §7 — o backend só aceita UM `stationId` por chamada (`Nexora.Api.Edge/Controllers/
 * KdsController.cs` exige `Guid stationId` não-nulo, não há "buscar tudo" no servidor). Este hook
 * chama `KdsQueueApi.queue()` uma vez POR id de `stationIds` (em paralelo) e mescla os resultados
 * client-side a cada `refresh()` — é o que alimenta tanto o modo "múltiplas praças numa tela"
 * quanto o modo "todas as praças" (basta passar `filter.selectedStationIds`, que já contém TODOS
 * os ids nesse caso — ver `useStationFilter`). Não inclui realtime/SignalR por design: fica a
 * cargo de quem integrar decidir se cada praça abre sua própria assinatura ou se o polling abaixo
 * (mesmo intervalo de fallback do ADR-011, 5s) basta — ver notas de integração em `station-filter.tsx`.
 */
export function useMultiStationKdsQueue(
  identity: Readonly<OperationalRequestIdentity>,
  stationIds: readonly string[],
  options: Readonly<{ baseUrl?: string; api?: KdsQueueApi; pollMs?: number }> = {},
): UseMultiStationKdsQueueResult {
  const { baseUrl = '', pollMs = 5000 } = options;
  const [api] = useState(() => options.api ?? new KdsQueueApi(baseUrl));
  const [items, setItems] = useState<readonly KdsQueueItemWithStation[] | null>(null);
  const [error, setError] = useState<string>();

  // Chave estável para efeitos — `stationIds` é recriado a cada render de quem chama.
  const stationIdsKey = stationIds.join(',');
  const stationIdsRef = useRef(stationIds);
  stationIdsRef.current = stationIds;

  const refresh = useCallback(async () => {
    const ids = stationIdsRef.current;
    if (ids.length === 0) return;
    try {
      const byStation = new Map<string, readonly KdsQueueItem[]>();
      await Promise.all(
        ids.map(async (stationId) => {
          const response = await api.queue(identity, stationId);
          byStation.set(stationId, response.items);
        }),
      );
      setItems(mergeKdsQueuesByStation(byStation));
      setError(undefined);
    } catch {
      // US-031 §9 (comportamento offline) — mantém a última fila conhecida em vez de limpar a tela.
      setError('Sem conexão com o servidor local — mostrando a última fila conhecida.');
    }
  }, [api, identity]);

  useEffect(() => {
    void refresh();
    if (pollMs <= 0) return;
    const interval = setInterval(() => void refresh(), pollMs);
    return () => clearInterval(interval);
  }, [refresh, stationIdsKey, pollMs]);

  const result = useMemo(() => ({ items, error, refresh }), [items, error, refresh]);
  return result;
}
