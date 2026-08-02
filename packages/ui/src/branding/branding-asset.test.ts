// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { validateBrandingAssetFile } from './branding-asset.js';

describe('validação local de asset de marca', () => {
  it('aceita PNG conferindo magic bytes', async () => {
    const file = new File(
      [new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])],
      'icon.png',
      { type: 'image/png' },
    );
    await expect(validateBrandingAssetFile(file, 'PWA_ICON')).resolves.toEqual({ valid: true });
  });

  it('rejeita extensão PNG com conteúdo falso', async () => {
    const file = new File(['not an image'], 'icon.png', { type: 'image/png' });
    await expect(validateBrandingAssetFile(file, 'PWA_ICON')).resolves.toEqual({
      valid: false,
      reason: 'Conteúdo do arquivo não corresponde ao formato informado.',
    });
  });

  it('rejeita SVG para ícone de PWA', async () => {
    const file = new File(['<svg xmlns="http://www.w3.org/2000/svg"></svg>'], 'icon.svg', {
      type: 'image/svg+xml',
    });
    await expect(validateBrandingAssetFile(file, 'PWA_ICON')).resolves.toEqual({
      valid: false,
      reason: 'Ícones PWA devem usar PNG ou WebP.',
    });
  });
});
