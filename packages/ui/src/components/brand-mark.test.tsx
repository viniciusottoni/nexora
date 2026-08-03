// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { BrandMark } from './brand-mark.js';

describe('BrandMark', () => {
  it('renderiza a imagem do logo quando logoSrc é informado', () => {
    render(<BrandMark logoSrc="/logo.png" tenantName="Dona Betinha" />);

    const img = screen.getByRole('img', { name: 'Dona Betinha' });
    expect(img).toHaveAttribute('src', '/logo.png');
  });

  it('sem logoSrc, renderiza a inicial do tenant e o wordmark em tipo', () => {
    render(<BrandMark tenantName="Dona Betinha" subtitle="Pizzaria" />);

    expect(screen.getByText('D')).toHaveClass('db-brand-mark__tenant');
    expect(screen.getByText('Dona Betinha')).toHaveClass('db-brand-mark__word');
    expect(screen.getByText('Pizzaria')).toHaveClass('db-brand-mark__sub');
  });

  it('sem tenantName nem logoSrc, desenha a marca Nexora colorida', () => {
    render(<BrandMark />);

    expect(screen.getByRole('img', { name: 'Nexora' }).tagName.toLowerCase()).toBe('svg');
    expect(screen.getByRole('img', { name: 'Nexora' })).not.toHaveClass('db-nexora-logo--white');
  });

  it('inverse troca a marca Nexora pela versão branca', () => {
    render(<BrandMark inverse />);

    expect(screen.getByRole('img', { name: 'Nexora' })).toHaveClass('db-nexora-logo--white');
  });
});
