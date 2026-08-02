import { useId, useState, type FormEvent } from 'react';
import { authResponseSchema } from '@nexora/contracts';
import { Button } from '../components/button.js';
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
  const refresh = await fetcher('/v1/auth/refresh', {
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

export function CloudLoginScreen({ onAuthenticated }: Readonly<{ onAuthenticated: () => void }>) {
  const emailId = useId();
  const passwordId = useId();
  const otpId = useId();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [otp, setOtp] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await cloudLogin({ email, password, ...(otp ? { otp } : {}) });
      onAuthenticated();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Não foi possível entrar.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="db-cloud-login">
      <form className="db-cloud-login__card" onSubmit={(event) => void submit(event)}>
        <p className="db-cloud-login__eyebrow">ACESSO SEGURO</p>
        <h1>Entrar na gestão</h1>
        <p>Use seu e-mail e senha. Informe o código de segurança quando solicitado.</p>
        {error ? <p role="alert">{error}</p> : null}
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
    </main>
  );
}

async function apiError(response: Response): Promise<Error> {
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  return new Error(problem?.detail ?? 'Não foi possível autenticar.');
}
