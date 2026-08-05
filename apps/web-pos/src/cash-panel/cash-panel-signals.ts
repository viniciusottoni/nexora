/**
 * "há 42 s" / "há 3 min" — mesmo formato textual de `SyncStatus.lastSync`
 * (`table-map/table-map-signals.ts#formatRelativeSync`), mas a partir de uma contagem de segundos
 * já resolvida pelo SERVIDOR (`OpenSessionEntry.waitingSeconds`), não de um timestamp local — o
 * caixa não deve recalcular "desde quando" a partir do relógio do navegador, porque
 * `billRequestedAt` pode ter chegado com sync atrasado (RN-020).
 */
export function formatWaitingSince(waitingSeconds: number): string {
  const seconds = Math.max(0, Math.round(waitingSeconds));
  if (seconds < 60) return `há ${seconds} s`;
  return `há ${Math.round(seconds / 60)} min`;
}

/** "Nenhum pendente" / "1 item pendente" / "3 itens pendentes" — rótulo de `OpenSessionEntry.pendingItems` (US-050 §7). */
export function formatPendingItems(count: number): string {
  if (count <= 0) return 'Nenhum pendente';
  return count === 1 ? '1 item pendente' : `${count} itens pendentes`;
}

/**
 * "1 sessão aberta" / "14 sessões abertas" / "1 sessão encontrada" / "0 sessões encontradas" —
 * subtítulo do card "Salão". Concordância de número (sessão/sessões) E de gênero/grau do adjetivo
 * (aberta/abertas, encontrada/encontradas) — um bug anterior gerava "1 sessão abertas" por
 * concatenar o sufixo sem flexionar junto com o substantivo.
 */
export function formatSessionsSubtitle(count: number, isSearchResult: boolean): string {
  const singular = count === 1;
  const noun = singular ? 'sessão' : 'sessões';
  let adjective: string;
  if (isSearchResult) {
    adjective = singular ? 'encontrada' : 'encontradas';
  } else {
    adjective = singular ? 'aberta' : 'abertas';
  }
  return `${count} ${noun} ${adjective}`;
}
