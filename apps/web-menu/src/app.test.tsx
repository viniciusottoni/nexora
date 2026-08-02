// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MenuHome } from './app.js';

describe('MenuHome', () => {
  it('renderiza nome, logo e texto vindos do branding', () => {
    render(
      <MenuHome
        tenantName="Casa do Bairro"
        welcome="Hoje tem comida boa"
        logo="https://cdn.example/logo.svg"
      />,
    );
    expect(screen.getByRole('heading', { name: 'Casa do Bairro' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Casa do Bairro' })).toHaveAttribute(
      'src',
      'https://cdn.example/logo.svg',
    );
    expect(screen.getByText('Hoje tem comida boa')).toBeInTheDocument();
  });
});
