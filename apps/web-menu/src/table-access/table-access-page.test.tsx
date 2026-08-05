// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createNeutralBrandingResponse, RuntimeBrandingProvider } from '@nexora/ui';
import { TableAccessPage } from './table-access-page.js';

const tableId = '0198aabb-2222-7000-8000-000000000002';
const sessionId = '0198aabb-3333-7000-8000-000000000003';

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function requestUrl(input: RequestInfo | URL) {
  if (typeof input === 'string') return input;
  if (input instanceof URL) return input.href;
  return input.url;
}

function createMemoryStorage(): Storage {
  const map = new Map<string, string>();
  return {
    getItem: (key) => map.get(key) ?? null,
    setItem: (key, value) => {
      map.set(key, value);
    },
    removeItem: (key) => map.delete(key) as unknown as void,
    clear: () => map.clear(),
    key: (index) => Array.from(map.keys())[index] ?? null,
    get length() {
      return map.size;
    },
  } satisfies Storage;
}

describe('TableAccessPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('mostra o esqueleto de carregamento antes de resolver a mesa (zero giro indefinido)', () => {
    const fetcher = vi.fn(() => new Promise<Response>(() => {})); // nunca resolve neste teste

    render(<TableAccessPage qrToken="token-mesa-12" fetcher={fetcher as unknown as typeof fetch} />);

    expect(screen.getByRole('status', { name: 'Carregando cardápio' })).toBeInTheDocument();
  });

  it('token invalido mostra mensagem amigavel, sem jargao tecnico (US-021 §4/§10)', async () => {
    const fetcher = vi.fn(async () =>
      jsonResponse({ detail: 'irrelevante', code: 'INVALID_TABLE_TOKEN' }, 404),
    );

    render(<TableAccessPage qrToken="token-invalido" fetcher={fetcher as unknown as typeof fetch} />);

    expect(
      await screen.findByText('Não conseguimos reconhecer esta mesa. Chame o garçom para continuar.'),
    ).toBeInTheDocument();
  });

  it('resolve a mesa, carrega o cardapio e guarda o sessionToken na storage', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => jsonResponse(createNeutralBrandingResponse())),
    );
    const storage = createMemoryStorage();
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url = requestUrl(input);
      if (url.includes('/public/table/')) {
        return jsonResponse({
          table: { id: tableId, label: '12', areaName: 'Salão' },
          session: {
            id: sessionId,
            tableId,
            tableLabel: '12',
            status: 'OPEN',
            openedAt: new Date().toISOString(),
            guestCount: 1,
            guestCountConfirmed: false,
            waiterId: null,
            source: 'QR',
            currentItems: [],
            total: '0.00',
          },
          sessionToken: 'jwt-de-sessao',
        });
      }
      return jsonResponse({ tenantId: sessionId, tenantName: 'Dona Betinha', categories: [] });
    });

    render(
      <RuntimeBrandingProvider fallback={createNeutralBrandingResponse()}>
        <TableAccessPage qrToken="token-mesa-12" fetcher={fetcher as unknown as typeof fetch} storage={storage} />
      </RuntimeBrandingProvider>,
    );

    expect(await screen.findByRole('heading', { name: 'Dona Betinha' })).toBeInTheDocument();
    await waitFor(() => expect(storage.getItem('nexora.web-menu.table-session.token-mesa-12')).toContain('jwt-de-sessao'));
  });
});
