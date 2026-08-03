import { useEffect, useId, useState, type FormEvent } from 'react';
import { authResponseSchema } from '@nexora/contracts';
import { NexoraSplash } from '../brand/nexora-loader.js';
import { NexoraLogo } from '../brand/nexora-logo.js';
import { Button } from '../components/button.js';
import { CreatedByFooter } from '../components/created-by-footer.js';
import { Field } from '../components/field.js';
import { Input } from '../components/input.js';

const ACCESS_KEY = 'food-operations.cloud.access';
const REFRESH_KEY = 'food-operations.cloud.refresh';

export function hasCloudSession(storage: Storage = localStorage): boolean {
  return Boolean(storage.getItem(ACCESS_KEY));
}

export function clearCloudSession(storage: Storage = localStorage): void {
  storage.removeItem(ACCESS_KEY);
  storage.removeItem(REFRESH_KEY);
}

export async function cloudLogin(
  input: { email: string; password: string; otp?: string },
  baseUrl = '',
  storage: Storage = localStorage,
  fetcher: typeof fetch = fetch,
): Promise<void> {
  const response = await fetcher(`${baseUrl}/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
    body: JSON.stringify(input),
  });
  if (!response.ok) throw await apiError(response);
  const session = authResponseSchema.parse(await response.json());
  storage.setItem(ACCESS_KEY, session.accessToken);
  if (session.refreshToken) storage.setItem(REFRESH_KEY, session.refreshToken);
}

export async function authenticatedFetch(
  input: RequestInfo | URL,
  init: RequestInit = {},
  storage: Storage = localStorage,
  fetcher: typeof fetch = fetch,
): Promise<Response> {
  const execute = (token: string | null) =>
    fetcher(input, {
      ...init,
      headers: { ...init.headers, ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    });
  let response = await execute(storage.getItem(ACCESS_KEY));
  if (response.status !== 401) return response;
  const refreshToken = storage.getItem(REFRESH_KEY);
  if (!refreshToken) return response;
  const baseUrl = deriveApiBaseUrl(input);
  const refresh = await fetcher(`${baseUrl}/v1/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
    body: JSON.stringify({ refreshToken }),
  });
  if (!refresh.ok) {
    clearCloudSession(storage);
    return response;
  }
  const renewed = authResponseSchema.parse(await refresh.json());
  storage.setItem(ACCESS_KEY, renewed.accessToken);
  if (renewed.refreshToken) storage.setItem(REFRESH_KEY, renewed.refreshToken);
  response = await execute(renewed.accessToken);
  return response;
}

function deriveApiBaseUrl(input: RequestInfo | URL): string {
  const path = requestPath(input);
  if (path.startsWith('/cloud/')) return '/cloud';
  if (path.startsWith('/edge/')) return '/edge';
  return '';
}

function requestPath(input: RequestInfo | URL): string {
  const base = globalThis.location?.origin ?? 'http://localhost';
  if (typeof input === 'string') return new URL(input, base).pathname;
  if (input instanceof URL) return input.pathname;
  return new URL(input.url, base).pathname;
}

function isLocalDevHost(): boolean {
  const host = globalThis.location?.hostname ?? '';
  return host === 'localhost' || host === '127.0.0.1';
}

export interface CloudLoginScreenProps {
  readonly onAuthenticated: () => void;
  readonly baseUrl?: string;
  readonly fetcher?: typeof fetch;
  readonly devOtpEnabled?: boolean;
}

export function CloudLoginScreen({
  onAuthenticated,
  baseUrl = '',
  fetcher = fetch,
  devOtpEnabled,
}: Readonly<CloudLoginScreenProps>) {
  const emailId = useId();
  const passwordId = useId();
  const otpId = useId();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [otp, setOtp] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [cardOpened, setCardOpened] = useState(false);
  const [devOtp, setDevOtp] = useState<string>();
  const [devOtpExpiresIn, setDevOtpExpiresIn] = useState<number>();
  const [devOtpError, setDevOtpError] = useState<string>();
  const showDevOtp = devOtpEnabled ?? (import.meta.env.DEV && isLocalDevHost());

  useEffect(() => {
    if (!showDevOtp) return undefined;

    let cancelled = false;

    async function loadDevOtp() {
      try {
        setDevOtpError(undefined);
        const response = await fetcher(`${baseUrl}/v1/dev/auth/otp`);
        if (!response.ok) {
          throw new Error('Falha ao consultar o OTP de teste.');
        }

        const payload = (await response.json()) as {
          otp?: string;
          expiresInSeconds?: number;
        };

        if (cancelled) return;
        setDevOtp(payload.otp);
        setDevOtpExpiresIn(payload.expiresInSeconds);
      } catch {
        if (cancelled) return;
        setDevOtp(undefined);
        setDevOtpExpiresIn(undefined);
        setDevOtpError('Nao foi possivel carregar o OTP de teste.');
      }
    }

    void loadDevOtp();
    const interval = globalThis.setInterval(() => void loadDevOtp(), 10_000);

    return () => {
      cancelled = true;
      globalThis.clearInterval(interval);
    };
  }, [baseUrl, fetcher, showDevOtp]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await cloudLogin({ email, password, ...(otp ? { otp } : {}) }, baseUrl, localStorage, fetcher);
      onAuthenticated();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Nao foi possivel entrar.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="db-cloud-login">
      <NexoraSplash label="Preparando o acesso" onOpened={() => setCardOpened(true)}>
        <form className="db-cloud-login__card" onSubmit={(event) => void submit(event)}>
          <NexoraLogo className="db-auth-logo" variant="lockup" height={42} shine />
          <p className="db-cloud-login__eyebrow">ACESSO SEGURO</p>
          <h1>Entrar na gestão</h1>
          <p>Use seu e-mail e senha. Informe o código de segurança quando solicitado.</p>
          {showDevOtp ? (
            <aside className="db-dev-otp" role="status" aria-live="polite">
              <p className="db-dev-otp__eyebrow">MODO DE TESTE</p>
              {devOtp ? (
                <strong>OTP atual: {devOtp}</strong>
              ) : (
                <strong>{devOtpError ?? 'Carregando OTP de teste...'}</strong>
              )}
              <small>
                {devOtpExpiresIn
                  ? `Atualiza em aproximadamente ${devOtpExpiresIn}s.`
                  : 'Visivel somente em ambiente local de desenvolvimento.'}
              </small>
            </aside>
          ) : null}
          {error ? (
            <p className="db-auth-error" role="alert" aria-live="polite">
              {error}
            </p>
          ) : null}
          <Field label="E-mail" htmlFor={emailId}>
            <Input
              id={emailId}
              type="email"
              autoComplete="username"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
          </Field>
          <Field label="Senha" htmlFor={passwordId}>
            <Input
              id={passwordId}
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />
          </Field>
          <Field label="Código de segurança (opcional)" htmlFor={otpId}>
            <Input
              id={otpId}
              inputMode="numeric"
              autoComplete="one-time-code"
              value={otp}
              onChange={(event) => setOtp(event.target.value.replace(/\D/g, '').slice(0, 6))}
            />
          </Field>
          <Button type="submit" busy={busy}>
            Entrar
          </Button>
        </form>
      </NexoraSplash>
      <CreatedByFooter
        className={`db-created-by--inline ${cardOpened ? 'is-visible' : ''}`.trim()}
      />
    </main>
  );
}

async function apiError(response: Response): Promise<Error> {
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  return new Error(problem?.detail ?? 'Nao foi possivel autenticar.');
}
