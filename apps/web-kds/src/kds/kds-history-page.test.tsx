// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { KdsHistoryPage } from './kds-history-page.js';

// stn=0198aabb-1111-7000-8000-000000000050 (payload de um dispositivo pareado à praça "forno") —
// mesmo formato de token de kds-queue-page.test.tsx.
const STATION_ID = '0198aabb-1111-7000-8000-000000000050';
function tokenWithStation(stationId: string | null): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  const payload: Record<string, unknown> = { sub: 'user-1' };
  if (stationId) payload.stn = stationId;
  return `${encode({ alg: 'HS256' })}.${encode(payload)}.sig`;
}

const identity = {
  accessToken: tokenWithStation(STATION_ID),
  deviceId: 'device-1',
  deviceSecret: 'secret-1',
};

function itemFixture(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    orderItemId: '0198aabb-1111-7000-8000-000000000001',
    orderId: '0198aabb-1111-7000-8000-0000000000a1',
    orderCode: '47',
    productName: 'Pizza Calabresa Grande',
    table: '12',
    firedAt: '2026-08-04T18:00:00.000Z',
    readyAt: '2026-08-04T18:09:06.000Z',
    servedAt: '2026-08-04T18:10:00.000Z',
    prepSeconds: 546,
    operator: { id: '0198aabb-1111-7000-8000-000000000099', name: 'Operador da Cozinha' },
    ...overrides,
  };
}

function historyResponse(items: readonly unknown[], summary = { count: items.length, avgPrepSeconds: 546 }) {
  return { ok: true, json: () => Promise.resolve({ items, summary }) };
}

describe('KdsHistoryPage (US-046)', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    cleanup();
  });

  it('mostra mensagem clara quando o terminal não tem praça associada', async () => {
    render(
      <KdsHistoryPage identity={{ ...identity, accessToken: tokenWithStation(null) }} onClose={() => {}} />,
    );

    expect(
      await screen.findByText(/não está associado a nenhuma praça de produção/i),
    ).toBeInTheDocument();
  });

  it('carrega e exibe os itens do turno com o resumo (contagem e tempo médio)', async () => {
    const fetchMock = vi.fn().mockResolvedValue(historyResponse([itemFixture()]));
    vi.stubGlobal('fetch', fetchMock);

    render(<KdsHistoryPage identity={identity} onClose={() => {}} />);

    await waitFor(() => expect(screen.getByTestId('kds-history-item')).toBeInTheDocument());
    expect(screen.getByText('#47')).toBeInTheDocument();
    expect(screen.getByText('Pizza Calabresa Grande')).toBeInTheDocument();
    expect(screen.getByText('Mesa 12')).toBeInTheDocument();
    expect(screen.getByText(/Operador da Cozinha/)).toBeInTheDocument();

    const summary = screen.getByTestId('kds-history-summary');
    expect(summary).toHaveTextContent('1');
    expect(summary).toHaveTextContent('9:06'); // 546s = 9min06s

    const [url] = fetchMock.mock.calls[0] as [string];
    expect(url).toContain(`stationId=${STATION_ID}`);
    expect(url).toContain('shift=current');
  });

  it('mostra estado vazio quando o turno não tem nenhum item concluído', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(historyResponse([], { count: 0, avgPrepSeconds: 0 })));

    render(<KdsHistoryPage identity={identity} onClose={() => {}} />);

    expect(await screen.findByText(/Nenhum item concluído neste turno ainda/i)).toBeInTheDocument();
  });

  it('busca por código curto envia o termo digitado ao servidor (debounced)', async () => {
    const fetchMock = vi.fn().mockResolvedValue(historyResponse([itemFixture()]));
    vi.stubGlobal('fetch', fetchMock);

    render(<KdsHistoryPage identity={identity} onClose={() => {}} />);
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));

    fireEvent.change(screen.getByTestId('kds-history-search'), { target: { value: '47' } });

    await waitFor(
      () => {
        const lastCall = fetchMock.mock.calls.at(-1) as [string];
        expect(lastCall[0]).toContain('search=47');
      },
      { timeout: 2000 },
    );
  });

  it('a tecla Escape devolve o operador à fila sem precisar de mouse', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(historyResponse([itemFixture()])));
    const onClose = vi.fn();

    render(<KdsHistoryPage identity={identity} onClose={onClose} />);
    await waitFor(() => expect(screen.getByTestId('kds-history-item')).toBeInTheDocument());

    fireEvent.keyDown(document, { key: 'Escape' });

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('o botão Voltar também devolve o operador à fila', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(historyResponse([itemFixture()])));
    const onClose = vi.fn();

    render(<KdsHistoryPage identity={identity} onClose={onClose} />);
    await waitFor(() => expect(screen.getByTestId('kds-history-item')).toBeInTheDocument());

    fireEvent.click(screen.getByTestId('kds-history-close'));

    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
