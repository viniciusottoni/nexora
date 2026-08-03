export interface OperationalRequestIdentity {
  readonly accessToken: string;
  readonly deviceId: string;
  readonly deviceSecret: string;
}

// Wrapper que RESOLVE `globalThis.fetch` A CADA CHAMADA — não `fetch.bind(globalThis)`:
// 1) um `fetch` capturado como valor (parâmetro default, propriedade de classe) e invocado depois
//    como `fetcher(...)` (chamada solta, sem receiver) perde a ligação com `window`/`globalThis`
//    que a implementação nativa exige como `this`, e lança "TypeError: Failed to execute 'fetch'
//    on 'Window': Illegal invocation" em navegador real (mascarado nos testes porque eles injetam
//    um `vi.fn()`, que não verifica `this`);
// 2) `.bind(globalThis)` resolveria (1), mas travaria a referência ao `fetch` ORIGINAL capturado
//    no momento em que o módulo carregou — `vi.stubGlobal('fetch', fetchMock)` dentro de um teste
//    reatribui `globalThis.fetch` DEPOIS desse carregamento, e o valor já congelado no `.bind`
//    nunca veria o mock (bug real observado: os testes de componente que confiam no `fetcher`
//    default paravam de bater o mock depois do `.bind`).
// A função abaixo resolve os dois: chama `globalThis.fetch(...)` como MÉTODO de `globalThis`
// (receiver correto) e faz essa resolução A CADA chamada (nunca cacheia o valor antigo).
const boundFetch: typeof fetch = (...args: Parameters<typeof fetch>) => globalThis.fetch(...args);

export async function operationalAuthenticatedFetch(
  input: RequestInfo | URL,
  init: RequestInit = {},
  identity: Readonly<OperationalRequestIdentity>,
  fetcher: typeof fetch = boundFetch,
): Promise<Response> {
  const headers = new Headers(init.headers);
  headers.set('Authorization', `Bearer ${identity.accessToken}`);
  headers.set('X-Device-Id', identity.deviceId);
  headers.set('X-Device-Secret', identity.deviceSecret);
  return fetcher(input, { ...init, headers });
}
