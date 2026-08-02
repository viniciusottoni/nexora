// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { BrandingResponse } from '@nexora/contracts';
import {
  applyBrandingToDocument,
  brandingCssProperties,
  createNeutralBrandingResponse,
  loadRuntimeBranding,
  type BrandingCache,
} from './runtime-branding.js';

const response: BrandingResponse = {
  tenant: { id: '0198a368-5ca3-7a0d-b231-183f77ae31a4', name: 'Casa do Bairro' },
  branding: {
    colors: { primary: '#174A3B', secondary: '#D79232', surface: '#F4F0E8', onPrimary: '#FFFFFF' },
    logo: { light: 'https://cdn.example/logo.svg' },
    favicon: 'https://cdn.example/favicon.png',
    fonts: { body: 'Manrope', display: 'Fraunces' },
    radius: 12,
    texts: {
      welcome: 'Bem-vindo',
      orderConfirmed: 'Pedido confirmado',
      thanks: 'Obrigado',
      terms: '',
    },
    pwa: { name: 'Casa do Bairro', shortName: 'Casa', themeColor: '#174A3B', icons: [] },
  },
  configVersion: 8,
};

describe('branding em runtime', () => {
  beforeEach(() => {
    document.documentElement.removeAttribute('style');
    document.head.innerHTML = '';
    document.title = '';
  });

  it('gera tokens CSS sem valor específico de tenant no build', () => {
    expect(brandingCssProperties(response.branding)).toMatchObject({
      '--brand-primary': '#174A3B',
      '--brand-on-primary': '#FFFFFF',
      '--brand-font': "'Manrope', system-ui, sans-serif",
      '--brand-radius': '12px',
    });
  });

  it('oferece fallback neutro sem marca de cliente', () => {
    const fallback = createNeutralBrandingResponse();
    expect(fallback.tenant.name).toBe('Estabelecimento');
    expect(fallback.branding.logo).toEqual({});
    expect(fallback.configVersion).toBe(1);
  });

  it('aplica tokens, favicon, manifest e título no documento', () => {
    applyBrandingToDocument(response, document);
    expect(document.documentElement.style.getPropertyValue('--brand-primary')).toBe('#174A3B');
    expect(document.querySelector<HTMLLinkElement>('link[rel="icon"]')?.href).toBe(
      response.branding.favicon,
    );
    expect(document.querySelector<HTMLLinkElement>('link[rel="manifest"]')?.href).toContain(
      '/v1/tenant/branding.webmanifest',
    );
    expect(document.title).toBe('Casa do Bairro');
  });

  it('usa cache imediatamente e atualiza pela rede', async () => {
    const applied: number[] = [];
    const cache = memoryCache({ ...response, configVersion: 7 });
    const remote = { ...response, configVersion: 8 };
    const loaded = await loadRuntimeBranding({
      host: 'menu.example.test',
      endpoint: '/v1/public/branding',
      fallback: response,
      cache,
      fetch: vi.fn(async () => new Response(JSON.stringify(remote), { status: 200 })),
      apply: (value) => applied.push(value.configVersion),
    });
    expect(applied).toEqual([7, 8]);
    expect(loaded.configVersion).toBe(8);
  });

  it('mantém cache expirado quando offline', async () => {
    const cached = { ...response, configVersion: 3 };
    const cache = memoryCache(cached, 0);
    const loaded = await loadRuntimeBranding({
      host: 'pos.example.test',
      endpoint: '/v1/public/branding',
      fallback: response,
      cache,
      now: () => 120_000,
      fetch: vi.fn(async () => {
        throw new TypeError('offline');
      }),
      apply: vi.fn(),
    });
    expect(loaded.configVersion).toBe(3);
  });

  it('degrada para tema neutro quando rede e cache falham', async () => {
    const apply = vi.fn();
    const loaded = await loadRuntimeBranding({
      host: 'kds.example.test',
      endpoint: '/v1/public/branding',
      fallback: response,
      cache: memoryCache(),
      fetch: vi.fn(async () => new Response('', { status: 503 })),
      apply,
    });
    expect(loaded).toBe(response);
    expect(apply).toHaveBeenCalledWith(response);
  });
});

function memoryCache(initial?: BrandingResponse, savedAt = Date.now()): BrandingCache {
  let value = initial ? { savedAt, value: initial } : undefined;
  return {
    read: () => value,
    write: (_host, next) => {
      value = next;
    },
  };
}
