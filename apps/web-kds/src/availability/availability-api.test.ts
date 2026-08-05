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
            unavailableReason: 'OUT_OF_STOCK',
            unavailableSince: '2026-08-02T20:00:00.000Z',
          }),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        ),
    );
    const api = new AvailabilityApi('/api', fetcher);

    const result = await api.markUnavailable(productId, 'OUT_OF_STOCK');

    expect(result.isAvailable).toBe(false);
    expect(fetcher.mock.calls[0]?.[0]).toBe(`/api/v1/kds/products/${productId}/unavailable`);
    const init = fetcher.mock.calls[0]?.[1];
    expect(new Headers(init?.headers).get('Idempotency-Key')).toBeTruthy();
    expect(JSON.parse(typeof init?.body === 'string' ? init.body : '')).toMatchObject({
      reason: 'OUT_OF_STOCK',
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

  it('preserva o receiver do fetch nativo quando nenhum fetcher e injetado', async () => {
    const originalFetch = globalThis.fetch;
    const guardedFetch = vi.fn(function (this: unknown) {
      if (this !== globalThis) throw new TypeError('Illegal invocation');
      return Promise.resolve(
        new Response(JSON.stringify({ items: [] }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      );
    }) as unknown as typeof fetch;
    globalThis.fetch = guardedFetch;

    try {
      await expect(new AvailabilityApi().listUnavailable()).resolves.toEqual({ items: [] });
      expect(guardedFetch).toHaveBeenCalledTimes(1);
    } finally {
      globalThis.fetch = originalFetch;
    }
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

  it('tenta reconectar o WebSocket com backoff 1s, 2s, 4s, 8s, 16s e teto de 30s', async () => {
    vi.useFakeTimers();
    try {
      const sockets: FakeWebSocket[] = [];

      subscribeToAvailability(vi.fn(), {
        webSocketFactory: () => {
          const socket = new FakeWebSocket();
          sockets.push(socket);
          return socket;
        },
        api: { listUnavailable: vi.fn().mockResolvedValue({ items: [] }) } as unknown as import('./availability-api.js').AvailabilityApi,
      });

      sockets[0]!.onclose?.();
      await vi.advanceTimersByTimeAsync(999);
      expect(sockets).toHaveLength(1);

      await vi.advanceTimersByTimeAsync(1);
      expect(sockets).toHaveLength(2);

      sockets[1]!.onclose?.();
      await vi.advanceTimersByTimeAsync(2000);
      expect(sockets).toHaveLength(3);

      sockets[2]!.onclose?.();
      await vi.advanceTimersByTimeAsync(4000);
      expect(sockets).toHaveLength(4);

      sockets[3]!.onclose?.();
      await vi.advanceTimersByTimeAsync(8000);
      expect(sockets).toHaveLength(5);

      sockets[4]!.onclose?.();
      await vi.advanceTimersByTimeAsync(16000);
      expect(sockets).toHaveLength(6);

      sockets[5]!.onclose?.();
      await vi.advanceTimersByTimeAsync(30000);
      expect(sockets).toHaveLength(7);
    } finally {
      vi.useRealTimers();
    }
  });

  it('encerra o polling quando o WebSocket reconecta', async () => {
    vi.useFakeTimers();
    try {
      const sockets: FakeWebSocket[] = [];
      const listUnavailable = vi.fn().mockResolvedValue({ items: [] });

      subscribeToAvailability(vi.fn(), {
        pollIntervalMs: 5000,
        webSocketFactory: () => {
          const socket = new FakeWebSocket();
          sockets.push(socket);
          return socket;
        },
        api: { listUnavailable } as unknown as import('./availability-api.js').AvailabilityApi,
      });

      sockets[0]!.onclose?.();
      await vi.advanceTimersByTimeAsync(1000);
      sockets[1]!.onopen?.();

      await vi.advanceTimersByTimeAsync(5000);

      expect(listUnavailable).not.toHaveBeenCalled();
    } finally {
      vi.useRealTimers();
    }
  });
});
