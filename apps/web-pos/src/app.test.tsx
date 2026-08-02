// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { PosHome } from './app.js';

describe('PosHome', () => {
  it('identifica operação com marca carregada em runtime', () => {
    render(<PosHome tenantName="Casa do Bairro" />);
    expect(screen.getByRole('heading', { name: 'Casa do Bairro' })).toBeInTheDocument();
    expect(screen.getByText('Caixa pronto')).toBeInTheDocument();
  });
});
