import { describe, expect, it } from 'vitest';
import { loadTableSession, saveTableSession, type StoredTableSession } from './session-storage.js';

function createMemoryStorage(): Storage {
  const map = new Map<string, string>();
  return {
    getItem: (key) => map.get(key) ?? null,
    setItem: (key, value) => {
      map.set(key, value);
    },
    removeItem: (key) => {
      map.delete(key);
    },
    clear: () => map.clear(),
    key: (index) => Array.from(map.keys())[index] ?? null,
    get length() {
      return map.size;
    },
  } satisfies Storage;
}

const session: StoredTableSession = {
  qrToken: 'token-mesa-12',
  sessionId: '0198aabb-1111-7000-8000-000000000001',
  tableId: '0198aabb-2222-7000-8000-000000000002',
  sessionToken: 'jwt-de-sessao',
  savedAt: '2026-08-02T20:12:04.221Z',
};

describe('session-storage (US-021, cenario "Retorno apos fechar o navegador")', () => {
  it('salva e recupera a sessao pelo qrToken', () => {
    const storage = createMemoryStorage();

    saveTableSession(storage, session);

    expect(loadTableSession(storage, 'token-mesa-12')).toEqual(session);
  });

  it('nao encontra sessao para um qrToken diferente (cada mesa tem sua propria entrada)', () => {
    const storage = createMemoryStorage();
    saveTableSession(storage, session);

    expect(loadTableSession(storage, 'token-de-outra-mesa')).toBeNull();
  });

  it('nao lanca quando o storage esta indisponivel/corrompido', () => {
    const throwingStorage: Storage = {
      getItem: () => {
        throw new Error('quota excedida');
      },
      setItem: () => {
        throw new Error('quota excedida');
      },
      removeItem: () => {},
      clear: () => {},
      key: () => null,
      length: 0,
    };

    expect(() => saveTableSession(throwingStorage, session)).not.toThrow();
    expect(loadTableSession(throwingStorage, session.qrToken)).toBeNull();
  });
});
