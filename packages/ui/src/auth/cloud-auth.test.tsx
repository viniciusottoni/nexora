// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { authenticatedFetch, CloudLoginScreen } from './cloud-auth.js';

describe('CloudLoginScreen', () => {
  beforeEach(() => {
    vi.spyOn(crypto, 'randomUUID').mockReturnValue('00000000-0000-4000-8000-000000000000');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('mostra o otp de teste quando o modo dev esta ativo', async () => {
    const fetcher = vi.fn(async () =>
      new Response(JSON.stringify({ otp: '123456', expiresInSeconds: 19 }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    render(<CloudLoginScreen onAuthenticated={vi.fn()} devOtpEnabled fetcher={fetcher as unknown as typeof fetch} />);

    expect(await screen.findByText('OTP atual: 123456')).toBeInTheDocument();
  });

  it('mostra o erro de autenticacao em bloco destacado', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ otp: '123456', expiresInSeconds: 19 }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ detail: 'Falha clara' }), {
          status: 401,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
      );

    render(<CloudLoginScreen onAuthenticated={vi.fn()} fetcher={fetcher as unknown as typeof fetch} />);

    fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: 'teste@nexora.local' } });
    fireEvent.change(screen.getByLabelText('Senha'), { target: { value: 'Senha1234!' } });
    fireEvent.submit(screen.getByRole('button', { name: 'Entrar' }).closest('form')!);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveClass('db-auth-error');
    expect(alert).toHaveTextContent('Falha clara');
  });

  it('renova a sessao usando o mesmo prefixo da requisicao original', async () => {
    const calls: string[] = [];
    const fetcher = vi.fn(async (input: RequestInfo | URL) => {
      const url =
        typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
      calls.push(url);
      if (calls.length === 1) return new Response('', { status: 401 });
      if (calls.length === 2) {
        return new Response(
          JSON.stringify({
            accessToken: 'novo-token',
            refreshToken: 'novo-refresh',
            user: { id: '44444444-4444-4444-8444-000000000001', name: 'Admin' },
            permissions: ['*'],
          }),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        );
      }
      return new Response('ok', { status: 200 });
    });
    const storage = {
      getItem: vi.fn((key: string) =>
        key === 'food-operations.cloud.access' ? 'token-antigo' : 'refresh-antigo',
      ),
      setItem: vi.fn(),
      removeItem: vi.fn(),
    } as unknown as Storage;

    await authenticatedFetch('/cloud/v1/devices', {}, storage, fetcher);

    expect(calls[1]).toBe('/cloud/v1/auth/refresh');
  });
});
