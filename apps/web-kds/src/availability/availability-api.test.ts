import { describe, expect, it, vi } from 'vitest';
import {
  AvailabilityApi,
  buildHandshakeFrame,
  splitHubFrames,
  subscribeToAvailability,
  type WebSocketLike,
} from './availability-api.js';

const productId = '0198aabb-1111-7000-8000-000000000001';

describe('AvailabilityApi', () => {
  it('marca indisponivel enviando motivo e Idempotency-Key nova', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(
          JSON.stringify({
            productId,
            productName: 'Pizza Calabresa',
            isAvailable: false,
            unavailableReason: 'Acabou o insumo',
            unavailableSince: '2026-08-02T20:00:00.000Z',
          }),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        ),
    );
    const api = new AvailabilityApi('/api', fetcher);

    const result = await api.markUnavailable(productId, 'Acabou o insumo');

    expect(result.isAvailable).toBe(false);
    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/kds/products/${productId}/unavailable`);
    const init = fetcher.mock.calls[0]?.[1];
    expect(new Headers(init?.headers).get('Idempotency-Key')).toBeTruthy();
    expect(JSON.parse(typeof init?.body === 'string' ? init.body : '')).toMatchObject({
      reason: 'Acabou o insumo',
      autoRestoreNextDay: true,
    });
  });

  it('lanca erro com o detail do problem+json quando a resposta falha', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ detail: 'Produto não encontrado.' }), {
          status: 404,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
    );
    const api = new AvailabilityApi('/api', fetcher);

    await expect(api.markAvailable('inexistente')).rejects.toThrow('Produto não encontrado.');
  });

  it('lista produtos indisponiveis', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ items: [] }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    const api = new AvailabilityApi('/api', fetcher);

    await expect(api.listUnavailable()).resolves.toEqual({ items: [] });
    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/v1/kds/products/unavailable');
  });

  it('envia o token operacional nas chamadas HTTP do KDS', async () => {
    const fetcher = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ items: [] }), { status: 200 }),
    );
    const api = new AvailabilityApi('/api', fetcher, 'jwt-kitchen');

    await api.listUnavailable();

    expect(new Headers(fetcher.mock.calls[0]?.[1]?.headers).get('Authorization')).toBe(
      'Bearer jwt-kitchen',
    );
  });
});

describe('protocolo do hub (framing)', () => {
  it('buildHandshakeFrame monta o frame JSON + separador de registro', () => {
    const frame = buildHandshakeFrame();
    expect(frame.endsWith(String.fromCharCode(0x1e))).toBe(true);
    expect(JSON.parse(frame.slice(0, -1))).toEqual({ protocol: 'json', version: 1 });
  });

  it('splitHubFrames separa multiplas mensagens completas no mesmo buffer', () => {
    const rs = String.fromCharCode(0x1e);
    const buffer = `${JSON.stringify({ a: 1 })}${rs}${JSON.stringify({ b: 2 })}${rs}`;

    const { messages, remainder } = splitHubFrames(buffer);

    expect(messages).toEqual([{ a: 1 }, { b: 2 }]);
    expect(remainder).toBe('');
  });

  it('splitHubFrames preserva um frame incompleto como remainder, para juntar com o proximo chunk', () => {
    const rs = String.fromCharCode(0x1e);
    const buffer = `${JSON.stringify({ a: 1 })}${rs}{"b":`;

    const { messages, remainder } = splitHubFrames(buffer);

    expect(messages).toEqual([{ a: 1 }]);
    expect(remainder).toBe('{"b":');
  });
});

class FakeWebSocket implements WebSocketLike {
  onopen: (() => void) | null = null;
  onmessage: ((event: { readonly data: string }) => void) | null = null;
  onclose: (() => void) | null = null;
  onerror: (() => void) | null = null;
  readonly sent: string[] = [];
  closed = false;

  send(data: string): void {
    this.sent.push(data);
  }

  close(): void {
    this.closed = true;
    this.onclose?.();
  }
}

describe('subscribeToAvailability', () => {
  it('envia o handshake ao abrir e entrega eventos de mudanca de disponibilidade recebidos do hub', () => {
    let socket: FakeWebSocket | undefined;
    const onChange = vi.fn();

    const subscription = subscribeToAvailability(onChange, {
      webSocketFactory: (url) => {
        socket = new FakeWebSocket();
        expect(url).toContain('/hubs/catalog-availability');
        return socket;
      },
    });

    socket!.onopen?.();
    expect(socket!.sent[0]).toBe(buildHandshakeFrame());

    const rs = String.fromCharCode(0x1e);
    const invocation = {
      type: 1,
      target: 'productAvailabilityChanged',
      arguments: [{ type: 'product.unavailable', data: { productId, reason: 'Acabou' } }],
    };
    socket!.onmessage?.({ data: `${JSON.stringify({})}${rs}${JSON.stringify(invocation)}${rs}` });

    expect(onChange).toHaveBeenCalledWith({
      type: 'product.unavailable',
      data: { productId, reason: 'Acabou' },
    });

    subscription.close();
    expect(socket!.closed).toBe(true);
  });

  it('cai para polling quando o WebSocket fecha, entregando a lista atual a cada tick', async () => {
    vi.useFakeTimers();
    try {
      let socket: FakeWebSocket | undefined;
      const onChange = vi.fn();
      const listUnavailable = vi.fn(async () => ({
        items: [
          {
            productId,
            productName: 'Pizza Calabresa',
            isAvailable: false,
            unavailableReason: 'Acabou',
            unavailableSince: '2026-08-02T20:00:00.000Z',
          },
        ],
      }));

      subscribeToAvailability(onChange, {
        pollIntervalMs: 5000,
        webSocketFactory: (_url) => {
          socket = new FakeWebSocket();
          return socket;
        },
        api: { listUnavailable } as unknown as import('./availability-api.js').AvailabilityApi,
      });

      socket!.onclose?.();

      await vi.advanceTimersByTimeAsync(5000);

      expect(listUnavailable).toHaveBeenCalledTimes(1);
      expect(onChange).toHaveBeenCalledWith({
        type: 'product.unavailable',
        data: { productId, reason: 'Acabou', unavailableSince: '2026-08-02T20:00:00.000Z' },
      });
    } finally {
      vi.useRealTimers();
    }
  });

  it('polling emite available quando um produto desaparece da lista de indisponiveis', async () => {
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
              productName: 'Pizza Calabresa',
              isAvailable: false,
              unavailableReason: 'Acabou',
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
        api: { listUnavailable } as unknown as import('./availability-api.js').AvailabilityApi,
      });

      socket!.onclose?.();
      await vi.advanceTimersByTimeAsync(10000);

      expect(onChange).toHaveBeenLastCalledWith({
        type: 'product.available',
        data: { productId },
      });
    } finally {
      vi.useRealTimers();
    }
  });
});
