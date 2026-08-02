// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { KdsHome } from './app.js';

describe('KdsHome', () => {
  it('identifica cozinha com marca carregada em runtime', () => {
    render(<KdsHome tenantName="Casa do Bairro" />);
    expect(screen.getByText('Casa do Bairro')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Cozinha em dia' })).toBeInTheDocument();
  });
});
