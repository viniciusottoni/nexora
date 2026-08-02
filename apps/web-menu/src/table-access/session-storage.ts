/**
 * Persistência do `sessionToken` anônimo (US-021 §4, cenário "Retorno após fechar o navegador":
 * "deve retornar à mesma sessão, com o consumo preservado").
 *
 * [DECISÃO/PWA differé] localStorage, não Dexie/IndexedDB: embora o enunciado desta tarefa
 * apontasse Dexie como dependência já presente no projeto, `apps/web-menu/package.json` não lista
 * Dexie hoje (confirmado por leitura direta) — introduzi-lo só para guardar um único token seria
 * uma dependência nova de build para um ganho pequeno nesta wave. localStorage já resolve os
 * cenários da US-021 (a chave é por `qrToken`, então qualquer mesa lida neste mesmo navegador tem
 * sua própria entrada). Uma limitação conhecida e aceita: Safari iOS pode limpar localStorage sob
 * pressão de armazenamento com mais agressividade que IndexedDB — migrar para Dexie fica
 * documentado como próximo passo de otimização de PWA (junto do cache de imagens via Service
 * Worker), não bloqueante para esta história.
 */
export interface StoredTableSession {
  readonly qrToken: string;
  readonly sessionId: string;
  readonly tableId: string;
  readonly sessionToken: string;
  readonly savedAt: string;
}

const KEY_PREFIX = 'nexora.web-menu.table-session.';

function keyFor(qrToken: string): string {
  return `${KEY_PREFIX}${qrToken}`;
}

export function saveTableSession(storage: Storage, session: StoredTableSession): void {
  try {
    storage.setItem(keyFor(session.qrToken), JSON.stringify(session));
  } catch {
    // Storage indisponível (modo privado, quota excedida) — a sessão continua funcionando na
    // aba atual; só o retorno após fechar o navegador fica prejudicado. Nunca quebra o fluxo.
  }
}

export function loadTableSession(storage: Storage, qrToken: string): StoredTableSession | null {
  try {
    const raw = storage.getItem(keyFor(qrToken));
    if (!raw) return null;
    return JSON.parse(raw) as StoredTableSession;
  } catch {
    return null;
  }
}
