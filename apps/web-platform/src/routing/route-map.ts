/**
 * US-150 §12 "Unitário: mapa de rotas, item ativo..." — lógica pura de roteamento do shell da
 * plataforma (sem DOM, sem fetch). A decisão de "renderizar, negar acesso ou voltar ao login"
 * (§12 "...e decisão de redirecionamento") depende da resposta do backend (sonda de
 * `/v1/platform/summary`, ver `PlatformAdminGate` em `app.tsx`) — coberta nos testes de integração
 * de `app.test.tsx`, não aqui.
 */

export type PlatformRouteId =
  | 'overview'
  | 'tenants'
  | 'tenants-new'
  | 'tenant-detail'
  | 'installations'
  | 'attention'
  | 'support-access'
  | 'business-templates'
  | 'releases'
  | 'onboarding';

export interface PlatformRouteMatch {
  readonly routeId: PlatformRouteId;
  readonly params: Readonly<Record<string, string>>;
}

interface RouteDefinition {
  readonly id: PlatformRouteId;
  readonly pattern: RegExp;
  readonly paramNames?: readonly string[];
}

// Ordem importa: /estabelecimentos/novo precisa casar antes do padrão genérico /estabelecimentos.
const ROUTES: readonly RouteDefinition[] = [
  { id: 'overview', pattern: /^\/$/ },
  { id: 'tenants-new', pattern: /^\/estabelecimentos\/novo\/?$/ },
  { id: 'tenants', pattern: /^\/estabelecimentos\/?$/ },
  // US-151 "cada linha deve abrir o detalhe do estabelecimento" — casa depois de tenants-new/
  // tenants (que exigem correspondência exata), então "/estabelecimentos/novo" continua caindo
  // na rota de criação, nunca aqui. Conteúdo do detalhe é a ficha administrativa da US-152 (ver
  // `TenantDetailPage`).
  { id: 'tenant-detail', pattern: /^\/estabelecimentos\/([^/]+)\/?$/, paramNames: ['tenantId'] },
  { id: 'installations', pattern: /^\/instalacoes\/?$/ },
  { id: 'attention', pattern: /^\/central-de-atencao\/?$/ },
  { id: 'support-access', pattern: /^\/auditoria-suporte\/?$/ },
  { id: 'business-templates', pattern: /^\/modelos-de-negocio\/?$/ },
  { id: 'releases', pattern: /^\/versoes\/?$/ },
  { id: 'onboarding', pattern: /^\/tenants\/([^/]+)\/onboarding\/?$/, paramNames: ['tenantId'] },
];

const PATH_BY_ROUTE: Record<Exclude<PlatformRouteId, 'onboarding' | 'tenant-detail'>, string> = {
  overview: '/',
  tenants: '/estabelecimentos',
  'tenants-new': '/estabelecimentos/novo',
  installations: '/instalacoes',
  attention: '/central-de-atencao',
  'support-access': '/auditoria-suporte',
  'business-templates': '/modelos-de-negocio',
  releases: '/versoes',
};

/** Rota canônica de um destino de navegação (não cobre rotas com parâmetro na URL: `onboarding`, `tenant-detail`). */
export function pathForRoute(
  routeId: Exclude<PlatformRouteId, 'onboarding' | 'tenant-detail'>,
): string {
  return PATH_BY_ROUTE[routeId];
}

/** US-151 — destino de uma linha do diretório. */
export function pathForTenantDetail(tenantId: string): string {
  return `/estabelecimentos/${tenantId}`;
}

/** Mantém a consulta reproduzível do diretório ao navegar para o detalhe e voltar (US-151 §3.1/§12). */
export function pathWithSearch(path: string, search: string): string {
  if (!search || search === '?') return path;
  return `${path}${search.startsWith('?') ? search : `?${search}`}`;
}

/** Casa um `pathname` contra o mapa de rotas — `undefined` quando nenhuma rota reconhece o caminho ("recurso inexistente"). */
export function matchRoute(pathname: string): PlatformRouteMatch | undefined {
  for (const route of ROUTES) {
    const match = route.pattern.exec(pathname);
    if (!match) continue;
    const params: Record<string, string> = {};
    (route.paramNames ?? []).forEach((name, index) => {
      params[name] = match[index + 1] ?? '';
    });
    return { routeId: route.id, params };
  }
  return undefined;
}

/**
 * Item do `SideNav` que deve aparecer marcado como ativo para uma rota — `tenants-new` realça
 * "Estabelecimentos" (US-150 §3.1: "'Novo estabelecimento' deve ser uma ação, não conteúdo único
 * da raiz" — não ganha entrada própria de navegação) e `onboarding` não pertence à navegação global.
 */
export function navItemIdForRoute(routeId: PlatformRouteId): string | undefined {
  if (routeId === 'tenants-new' || routeId === 'tenant-detail') return 'tenants';
  if (routeId === 'onboarding') return undefined;
  return routeId;
}
