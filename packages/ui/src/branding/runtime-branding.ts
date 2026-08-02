import type { Branding, BrandingResponse } from '@nexora/contracts';

export type BrandingCssProperties = Readonly<Record<`--${string}`, string>>;

export interface CachedBranding {
  readonly savedAt: number;
  readonly value: BrandingResponse;
}

export interface BrandingCache {
  read(host: string): CachedBranding | undefined;
  write(host: string, value: CachedBranding): void;
}

export interface LoadRuntimeBrandingOptions {
  readonly host: string;
  readonly endpoint: string;
  readonly fallback: BrandingResponse;
  readonly cache: BrandingCache;
  readonly fetch: typeof globalThis.fetch;
  readonly apply: (value: BrandingResponse) => void;
  readonly now?: () => number;
}

const CACHE_PREFIX = 'runtime-branding:';

/**
 * Tema neutro de fallback (cenário Gherkin "Ausência de configuração" da US-003) — usado apenas
 * enquanto um tenant não configurou marca própria, ou quando rede e cache falham
 * simultaneamente (ver {@link loadRuntimeBranding}). Paleta genuinamente neutra, sem qualquer
 * associação a um cliente real (ADR-013): mesma paleta de referência de
 * `Nexora.Application.Branding.BrandingDefaults` (backend/src/Nexora.Application/Branding/BrandingDefaults.cs)
 * — as duas pontas foram unificadas para não haver paletas "neutras" divergentes.
 */
export function createNeutralBrandingResponse(): BrandingResponse {
  return {
    tenant: { id: '00000000-0000-0000-0000-000000000000', name: 'Estabelecimento' },
    branding: {
      colors: {
        primary: '#1E3446',
        secondary: '#B8965A',
        surface: '#F7F2E8',
        onPrimary: '#FFFFFF',
      },
      logo: {},
      fonts: { body: 'Inter', display: 'EB Garamond' },
      radius: 8,
      texts: {
        welcome: 'Bem-vindo',
        orderConfirmed: 'Pedido confirmado.',
        thanks: 'Obrigado pela preferência.',
        terms: '',
      },
      pwa: { name: 'Estabelecimento', shortName: 'Operação', themeColor: '#1E3446', icons: [] },
    },
    configVersion: 1,
  };
}

export function brandingCssProperties(branding: Branding): BrandingCssProperties {
  return {
    '--brand-primary': branding.colors.primary,
    '--brand-on-primary': branding.colors.onPrimary,
    '--brand-secondary': branding.colors.secondary,
    '--brand-surface': branding.colors.surface,
    '--brand-font': fontStack(branding.fonts.body, 'sans-serif'),
    '--brand-radius': `${branding.radius}px`,
  };
}

export function applyBrandingToDocument(
  value: BrandingResponse,
  target: Document = document,
): void {
  for (const [property, content] of Object.entries(brandingCssProperties(value.branding))) {
    target.documentElement.style.setProperty(property, content);
  }
  target.title = value.branding.pwa.name || value.tenant.name;
  upsertLink(target, 'icon', value.branding.favicon);
  const host = target.location?.host;
  const manifestPath = `/v1/tenant/branding.webmanifest${host ? `?host=${encodeURIComponent(host)}` : ''}`;
  upsertLink(target, 'manifest', manifestPath);
  upsertMeta(target, 'theme-color', value.branding.pwa.themeColor);
}

export async function loadRuntimeBranding(
  options: LoadRuntimeBrandingOptions,
): Promise<BrandingResponse> {
  const now = options.now ?? Date.now;
  const cached = safeRead(options.cache, options.host);
  if (cached) options.apply(cached.value);

  try {
    const separator = options.endpoint.includes('?') ? '&' : '?';
    const response = await options.fetch(
      `${options.endpoint}${separator}host=${encodeURIComponent(options.host)}`,
      {
        headers: { accept: 'application/json' },
        cache: 'no-cache',
      },
    );
    if (!response.ok) throw new Error(`Branding indisponível (${response.status})`);
    const remote = (await response.json()) as BrandingResponse;
    if (!isBrandingResponse(remote)) throw new Error('Resposta de branding inválida');
    options.cache.write(options.host, { savedAt: now(), value: remote });
    options.apply(remote);
    return remote;
  } catch {
    if (cached) return cached.value;
    options.apply(options.fallback);
    return options.fallback;
  }
}

export function createLocalStorageBrandingCache(
  storage: Pick<Storage, 'getItem' | 'setItem'>,
): BrandingCache {
  return {
    read(host) {
      const serialized = storage.getItem(`${CACHE_PREFIX}${host}`);
      if (!serialized) return undefined;
      try {
        const parsed = JSON.parse(serialized) as CachedBranding;
        return typeof parsed.savedAt === 'number' && isBrandingResponse(parsed.value)
          ? parsed
          : undefined;
      } catch {
        return undefined;
      }
    },
    write(host, value) {
      storage.setItem(`${CACHE_PREFIX}${host}`, JSON.stringify(value));
    },
  };
}

function safeRead(cache: BrandingCache, host: string): CachedBranding | undefined {
  try {
    return cache.read(host);
  } catch {
    return undefined;
  }
}

function isBrandingResponse(value: unknown): value is BrandingResponse {
  if (!value || typeof value !== 'object') return false;
  const candidate = value as Partial<BrandingResponse>;
  return (
    typeof candidate.configVersion === 'number' &&
    typeof candidate.tenant?.name === 'string' &&
    typeof candidate.branding?.colors?.primary === 'string'
  );
}

function upsertLink(target: Document, relation: string, href: string | undefined): void {
  if (!href) return;
  let link = target.querySelector<HTMLLinkElement>(`link[rel="${relation}"]`);
  if (!link) {
    link = target.createElement('link');
    link.rel = relation;
    target.head.append(link);
  }
  link.href = href;
}

function upsertMeta(target: Document, name: string, content: string): void {
  let meta = target.querySelector<HTMLMetaElement>(`meta[name="${name}"]`);
  if (!meta) {
    meta = target.createElement('meta');
    meta.name = name;
    target.head.append(meta);
  }
  meta.content = content;
}

function fontStack(name: string, fallback: 'sans-serif' | 'serif'): string {
  return `'${name.replaceAll("'", '')}', system-ui, ${fallback}`;
}
