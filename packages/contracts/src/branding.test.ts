import { describe, expect, it } from 'vitest';

import {
  brandingResponseSchema,
  updateBrandingRequestSchema,
  uploadBrandingAssetRequestSchema,
} from './branding.js';

describe('contratos de branding', () => {
  it('aceita resposta pública completa', () => {
    const parsed = brandingResponseSchema.parse({
      tenant: { id: '0198a368-5ca3-7a0d-b231-183f77ae31a4', name: 'Casa do Bairro' },
      branding: {
        colors: {
          primary: '#174A3B',
          secondary: '#D79232',
          surface: '#F4F0E8',
          onPrimary: '#FFFFFF',
        },
        logo: {},
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
      configVersion: 88,
    });
    expect(parsed.configVersion).toBe(88);
  });

  it('rejeita cor que não seja hexadecimal de seis dígitos', () => {
    expect(() => updateBrandingRequestSchema.parse({ colors: { primary: 'verde' } })).toThrow();
  });

  it('aceita apenas tipos e tamanhos permitidos no início de upload', () => {
    expect(
      uploadBrandingAssetRequestSchema.safeParse({
        kind: 'LOGO_LIGHT',
        contentType: 'image/svg+xml',
        bytes: 512,
        sha256: 'a'.repeat(64),
      }).success,
    ).toBe(true);
    expect(
      uploadBrandingAssetRequestSchema.safeParse({
        kind: 'PWA_ICON',
        contentType: 'application/pdf',
        bytes: 512,
        sha256: 'a'.repeat(64),
      }).success,
    ).toBe(false);
    expect(
      uploadBrandingAssetRequestSchema.safeParse({
        kind: 'PWA_ICON',
        contentType: 'image/png',
        bytes: 10_000_001,
        sha256: 'a'.repeat(64),
      }).success,
    ).toBe(false);
  });
});
