export interface OperationalAuthClientOptions {
  readonly baseUrl: string;
  readonly deviceId: string;
  readonly deviceSecret: string;
  readonly fetch?: typeof globalThis.fetch;
  readonly storage?: Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;
  readonly createKey?: () => string;
}

export interface OperationalSession {
  readonly accessToken: string;
  readonly user: Readonly<{ id: string; name: string }>;
  readonly permissions: readonly string[];
}

export interface AuthorizationGrant {
  readonly authorizationToken: string;
  readonly expiresIn: number;
  readonly authorizedBy: Readonly<{ id: string; name: string }>;
}

export class OperationalAuthError extends Error {
  constructor(
    readonly code: string,
    message: string,
    readonly retryAfterSeconds?: number,
  ) {
    super(message);
  }
}

export class OperationalAuthClient {
  private readonly request: typeof globalThis.fetch;
  private readonly storage: Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;
  private readonly createKey: () => string;
  private accessToken?: string;

  constructor(private readonly options: OperationalAuthClientOptions) {
    this.request = options.fetch ?? globalThis.fetch.bind(globalThis);
    this.storage = options.storage ?? globalThis.localStorage;
    this.createKey = options.createKey ?? (() => crypto.randomUUID());
  }

  async loginWithPin(pin: string, intentionId: string): Promise<OperationalSession> {
    const session = await this.post<OperationalSession>(
      '/v1/auth/pin',
      { pin, deviceId: this.options.deviceId },
      intentionId,
    );
    this.accessToken = session.accessToken;
    return session;
  }

  async authorize(
    action: string,
    pin: string,
    context: Readonly<Record<string, unknown>>,
    intentionId: string,
  ): Promise<AuthorizationGrant> {
    return this.post('/v1/auth/authorize', { action, pin, context }, intentionId, this.accessToken);
  }

  async executeSensitive<TResult>(
    path: string,
    body: unknown,
    intentionId: string,
    authorizationToken: string,
  ): Promise<TResult> {
    return this.post(path, body, intentionId, this.accessToken, authorizationToken);
  }

  private async post<TResult>(
    path: string,
    body: unknown,
    intentionId: string,
    bearer?: string,
    authorizationToken?: string,
  ): Promise<TResult> {
    const storageKey = `idempotency:${path}:${intentionId}`;
    let idempotencyKey = this.storage.getItem(storageKey);
    if (!idempotencyKey) {
      idempotencyKey = this.createKey();
      this.storage.setItem(storageKey, idempotencyKey);
    }
    const response = await this.request(`${this.options.baseUrl}${path}`, {
      method: 'POST',
      headers: {
        'content-type': 'application/json',
        'Idempotency-Key': idempotencyKey,
        'X-Device-Id': this.options.deviceId,
        'X-Device-Secret': this.options.deviceSecret,
        ...(bearer ? { authorization: `Bearer ${bearer}` } : {}),
        ...(authorizationToken ? { 'X-Authorization-Token': authorizationToken } : {}),
      },
      body: JSON.stringify(body),
    });
    const payload = await readJson(response);
    if (!response.ok) {
      const problem = payload as {
        code?: string;
        detail?: string;
        meta?: { retryAfterSeconds?: number };
      };
      throw new OperationalAuthError(
        problem.code ?? 'INTERNAL_ERROR',
        problem.detail ?? 'Não foi possível concluir.',
        problem.meta?.retryAfterSeconds,
      );
    }
    this.storage.removeItem(storageKey);
    return payload as TResult;
  }
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text();
  try {
    return text ? JSON.parse(text) : {};
  } catch {
    return {};
  }
}
