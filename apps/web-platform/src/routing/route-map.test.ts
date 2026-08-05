import { describe, expect, it } from 'vitest';

import { matchRoute, navItemIdForRoute, pathForRoute, pathForTenantDetail } from './route-map.js';

describe('matchRoute', () => {
  it.each([
    ['/', 'overview'],
    ['/estabelecimentos', 'tenants'],
    ['/estabelecimentos/', 'tenants'],
    ['/estabelecimentos/novo', 'tenants-new'],
    ['/instalacoes', 'installations'],
    ['/auditoria-suporte', 'support-access'],
    ['/modelos-de-negocio', 'business-templates'],
    ['/versoes', 'releases'],
  ] as const)('reconhece %s como %s', (pathname, routeId) => {
    expect(matchRoute(pathname)?.routeId).toBe(routeId);
  });

  it('extrai o tenantId da rota de onboarding', () => {
    expect(matchRoute('/tenants/abc-123/onboarding')).toEqual({
      routeId: 'onboarding',
      params: { tenantId: 'abc-123' },
    });
  });

  it('"/estabelecimentos/novo" nunca casa com o padrão genérico de estabelecimentos', () => {
    expect(matchRoute('/estabelecimentos/novo')?.routeId).toBe('tenants-new');
  });

  it('extrai o tenantId da rota de detalhe do diretório (US-151)', () => {
    expect(matchRoute('/estabelecimentos/abc-123')).toEqual({
      routeId: 'tenant-detail',
      params: { tenantId: 'abc-123' },
    });
  });

  it('"/estabelecimentos" sem id continua casando com o diretório, não com o detalhe', () => {
    expect(matchRoute('/estabelecimentos')?.routeId).toBe('tenants');
    expect(matchRoute('/estabelecimentos/')?.routeId).toBe('tenants');
  });

  it('retorna undefined para caminho desconhecido (recurso inexistente)', () => {
    expect(matchRoute('/rota-que-nao-existe')).toBeUndefined();
  });
});

describe('pathForTenantDetail', () => {
  it('é a inversa de matchRoute para a rota de detalhe', () => {
    expect(matchRoute(pathForTenantDetail('abc-123'))).toEqual({
      routeId: 'tenant-detail',
      params: { tenantId: 'abc-123' },
    });
  });
});

describe('pathForRoute', () => {
  it('é a inversa de matchRoute para rotas sem parâmetro', () => {
    for (const routeId of ['overview', 'tenants', 'tenants-new', 'installations', 'support-access', 'business-templates', 'releases'] as const) {
      expect(matchRoute(pathForRoute(routeId))?.routeId).toBe(routeId);
    }
  });
});

describe('navItemIdForRoute', () => {
  it('realça "tenants" no diretório, no formulário de novo estabelecimento e no detalhe', () => {
    expect(navItemIdForRoute('tenants')).toBe('tenants');
    expect(navItemIdForRoute('tenants-new')).toBe('tenants');
    expect(navItemIdForRoute('tenant-detail')).toBe('tenants');
  });

  it('onboarding não pertence à navegação global', () => {
    expect(navItemIdForRoute('onboarding')).toBeUndefined();
  });

  it('demais rotas realçam a si mesmas', () => {
    expect(navItemIdForRoute('overview')).toBe('overview');
    expect(navItemIdForRoute('installations')).toBe('installations');
  });
});
