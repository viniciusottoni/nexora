// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { BrandingPreview } from './branding-preview.js';

describe('BrandingPreview', () => {
  it('pré-visualiza marca e texto antes de salvar', () => {
    render(
      <BrandingPreview
        tenantName="Casa do Bairro"
        welcome="Bem-vindo à casa"
        primary="#174A3B"
        onPrimary="#FFFFFF"
        surface="#F4F0E8"
        radius={12}
      />,
    );
    expect(
      screen.getByRole('region', { name: 'Pré-visualização da identidade visual' }),
    ).toHaveTextContent('Casa do Bairro');
    expect(screen.getByText('Bem-vindo à casa')).toBeInTheDocument();
  });
});
