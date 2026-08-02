import { describe, expect, it, vi } from 'vitest';
import {
  AvailabilityApi,
  subscribeToAvailability,
  type WebSocketLike,
} from './availability-api.js';

const productId = '0198aabb-1111-7000-8000-000000000001';

class FakeWebSocket implements WebSocketLike {
  onopen: (() => void) | null = null;
  onmessage: ((event: { readonly data: string }) => void) | null = null;
  onclose: (() => void) | null = null;
  onerror: (() => void) | null = null;
  send(): void {}
  close(): void {
    this.onclose?.();
  }
}

describe('AvailabilityApi do painel', () => {
  it('valida a resposta e envia idempotência ao alterar disponibilidade', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(
          JSON.stringify({
            productId,
            productName: 'Pizza',
            isAvailable: false,
            unavailableReason: 'Sem insumo',
            unavailableSince: '2026-08-02T20:00:00.000Z',
          }),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        ),
    );
    const api = new AvailabilityApi('/api', fetcher);

    await expect(api.markUnavailable(productId, 'Sem insumo')).resolves.toMatchObject({
      productId,
    });
    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/catalog/products/${productId}/availability`);
    expect(new Headers(fetcher.mock.calls[0]?.[1]?.headers).get('Idempotency-Key')).toBeTruthy();
  });

  it('polling informa retorno à disponibilidade quando o item some da lista', async () => {
    vi.useFakeTimers();
    try {
      let socket: FakeWebSocket | undefined;
      const onChange = vi.fn();
      const listUnavailable = vi
        .fn()
        .mockResolvedValueOnce({
          items: [
            {
              productId,
              productName: 'Pizza',
              isAvailable: false,
              unavailableReason: 'Sem insumo',
              unavailableSince: '2026-08-02T20:00:00.000Z',
            },
          ],
        })
        .mockResolvedValueOnce({ items: [] });

      subscribeToAvailability(onChange, {
        pollIntervalMs: 5000,
        webSocketFactory: () => {
          socket = new FakeWebSocket();
          return socket;
        },
        api: { listUnavailable } as unknown as AvailabilityApi,
      });

      socket!.onclose?.();
      await vi.advanceTimersByTimeAsync(10_000);

      expect(onChange).toHaveBeenLastCalledWith({
        type: 'product.available',
        data: { productId },
      });
    } finally {
      vi.useRealTimers();
    }
  });
});
