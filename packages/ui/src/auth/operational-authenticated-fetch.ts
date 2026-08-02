export interface OperationalRequestIdentity {
  readonly accessToken: string;
  readonly deviceId: string;
  readonly deviceSecret: string;
}

export async function operationalAuthenticatedFetch(
  input: RequestInfo | URL,
  init: RequestInit = {},
  identity: Readonly<OperationalRequestIdentity>,
  fetcher: typeof fetch = fetch,
): Promise<Response> {
  const headers = new Headers(init.headers);
  headers.set('Authorization', `Bearer ${identity.accessToken}`);
  headers.set('X-Device-Id', identity.deviceId);
  headers.set('X-Device-Secret', identity.deviceSecret);
  return fetcher(input, { ...init, headers });
}
