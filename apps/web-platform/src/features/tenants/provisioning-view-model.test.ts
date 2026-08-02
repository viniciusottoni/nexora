import { describe, expect, it } from 'vitest';

import { deriveSlugSuggestion, maskInstallationCommand } from './provisioning-view-model.js';

describe('deriveSlugSuggestion', () => {
  it('atualiza a sugestao enquanto o slug ainda nao foi editado', () => {
    expect(deriveSlugSuggestion('Pizzaria Dona Betinha', '', false)).toBe('pizzaria-dona-betinha');
  });

  it('preserva o slug informado manualmente', () => {
    expect(deriveSlugSuggestion('Outro nome', 'slug-escolhido', true)).toBe('slug-escolhido');
  });
});

describe('maskInstallationCommand', () => {
  it('oculta o token de uso unico quando a tela perde a revelacao', () => {
    expect(maskInstallationCommand('./install.sh --tenant=abc --token=segredo')).toBe(
      './install.sh --tenant=abc --token=••••••••',
    );
  });
});
