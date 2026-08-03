// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CancelOrderItemFlow } from './cancel-order-item-flow.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };
const orderId = '0198aabb-1111-7000-8000-000000000001';
const itemId = '0198aabb-2222-7000-8000-000000000002';

function jsonResponse(body: unknown, status = 200) {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  } as Response;
}

function cancelledItemResponse() {
  return {
    item: {
      id: itemId,
      status: 'CANCELLED',
      cancelledAt: new Date().toISOString(),
      reason: 'CUSTOMER_REQUEST',
      notes: null,
      wasStarted: false,
      authorizedBy: null,
    },
  };
}

/**
 * US-033 (Cancelar item ou pedido com autorização) §7/§10/§12 ("Componente: vitest do modal de
 * autorização no fluxo de cancelamento") — cobre os dois caminhos do fluxo do lado do cliente:
 * cancelamento direto (item na fila) e o desvio de autorização pontual (item já iniciado, ADR-023).
 */
describe('CancelOrderItemFlow (US-033)', () => {
  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it('exige motivo antes de habilitar "Confirmar cancelamento"', () => {
    render(
      <CancelOrderItemFlow
        identity={identity}
        orderId={orderId}
        itemId={itemId}
        itemName="Pizza Margherita"
        onCancelled={() => {}}
      />,
    );

    expect(screen.getByRole('button', { name: 'Confirmar cancelamento' })).toBeDisabled();

    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'CUSTOMER_REQUEST' } });

    expect(screen.getByRole('button', { name: 'Confirmar cancelamento' })).toBeEnabled();
  });

  it('cancela direto quando o item ainda está na fila (sem autorização) e avisa o chamador', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse(cancelledItemResponse(), 200));
    const onCancelled = vi.fn();

    render(
      <CancelOrderItemFlow
        identity={identity}
        orderId={orderId}
        itemId={itemId}
        itemName="Pizza Margherita"
        fetcher={fetchMock as unknown as typeof fetch}
        onCancelled={onCancelled}
      />,
    );

    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'CUSTOMER_REQUEST' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelamento' }));

    await waitFor(() => expect(onCancelled).toHaveBeenCalledTimes(1));

    const [, cancelInit] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(new Headers(cancelInit.headers).get('X-Authorization-Token')).toBeNull();
  });

  it('403 AUTHORIZATION_REQUIRED abre o diálogo de PIN do gerente e repete o cancelamento com o token', async () => {
    const fetchMock = vi
      .fn()
      // 1ª chamada: PATCH .../cancel sem token — item já iniciado, servidor recusa.
      .mockResolvedValueOnce(
        jsonResponse(
          {
            code: 'AUTHORIZATION_REQUIRED',
            detail: 'Item já iniciado. É necessária autorização de perfil superior.',
            meta: { action: 'CANCEL_STARTED_ITEM', itemStatus: 'FIRED' },
          },
          403,
        ),
      )
      // 2ª chamada: POST /v1/auth/authorize com o PIN do gerente.
      .mockResolvedValueOnce(
        jsonResponse(
          {
            authorizationToken: 'authz-token-abc',
            expiresIn: 120,
            authorizedBy: { id: '0198aabb-3333-7000-8000-000000000003', name: 'Gerente Ana' },
          },
          200,
        ),
      )
      // 3ª chamada: PATCH .../cancel de novo, agora com X-Authorization-Token.
      .mockResolvedValueOnce(jsonResponse(cancelledItemResponse(), 200));

    const onCancelled = vi.fn();

    render(
      <CancelOrderItemFlow
        identity={identity}
        orderId={orderId}
        itemId={itemId}
        itemName="Pizza Calabresa"
        fetcher={fetchMock as unknown as typeof fetch}
        onCancelled={onCancelled}
      />,
    );

    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'CUSTOMER_REQUEST' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelamento' }));

    // ADR-023: 403 abre o diálogo de PIN no MESMO dispositivo do garçom.
    expect(await screen.findByRole('heading', { name: 'Autorização necessária' })).toBeInTheDocument();
    expect(screen.getByText(/item já em produção/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: '9' }));
    fireEvent.click(screen.getByRole('button', { name: '9' }));
    fireEvent.click(screen.getByRole('button', { name: '1' }));
    fireEvent.click(screen.getByRole('button', { name: '1' }));
    fireEvent.click(screen.getByRole('button', { name: 'Autorizar' }));

    await waitFor(() => expect(onCancelled).toHaveBeenCalledTimes(1));
    expect(fetchMock).toHaveBeenCalledTimes(3);

    const [authorizeUrl, authorizeInit] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(authorizeUrl).toContain('/v1/auth/authorize');
    const authorizeBody = JSON.parse(authorizeInit.body as string) as {
      action: string;
      pin: string;
      context: { orderItemId: string };
    };
    expect(authorizeBody.action).toBe('CANCEL_STARTED_ITEM');
    expect(authorizeBody.pin).toBe('9911');
    expect(authorizeBody.context.orderItemId).toBe(itemId);

    const [, retryInit] = fetchMock.mock.calls[2] as [string, RequestInit];
    expect(new Headers(retryInit.headers).get('X-Authorization-Token')).toBe('authz-token-abc');
  });

  it('PIN inválido mantém o diálogo aberto com o erro, sem cancelar o item', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        jsonResponse({ code: 'AUTHORIZATION_REQUIRED', detail: 'Autorização necessária.' }, 403),
      )
      .mockResolvedValueOnce(
        jsonResponse({ code: 'AUTH_INVALID_CREDENTIALS', detail: 'PIN incorreto.' }, 401),
      );

    const onCancelled = vi.fn();

    render(
      <CancelOrderItemFlow
        identity={identity}
        orderId={orderId}
        itemId={itemId}
        itemName="Pizza Calabresa"
        fetcher={fetchMock as unknown as typeof fetch}
        onCancelled={onCancelled}
      />,
    );

    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'CUSTOMER_REQUEST' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelamento' }));

    await screen.findByRole('heading', { name: 'Autorização necessária' });

    fireEvent.click(screen.getByRole('button', { name: '0' }));
    fireEvent.click(screen.getByRole('button', { name: '0' }));
    fireEvent.click(screen.getByRole('button', { name: '0' }));
    fireEvent.click(screen.getByRole('button', { name: '0' }));
    fireEvent.click(screen.getByRole('button', { name: 'Autorizar' }));

    expect(await screen.findByText('PIN incorreto.')).toBeInTheDocument();
    expect(onCancelled).not.toHaveBeenCalled();
  });
});
