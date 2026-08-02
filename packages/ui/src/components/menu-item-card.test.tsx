// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MenuItemCard } from './menu-item-card.js';

describe('MenuItemCard', () => {
  it('mostra o placeholder textual explícito quando não há foto do produto', () => {
    render(<MenuItemCard name="Calabresa G" price="R$ 64,90" />);

    expect(screen.getByText('foto do produto — a fornecer pelo estabelecimento')).toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });

  it('desabilita e sinaliza item esgotado', () => {
    render(<MenuItemCard name="Portuguesa G" price="R$ 72,00" unavailable />);

    const button = screen.getByRole('button', { name: /Portuguesa G/ });
    expect(button).toBeDisabled();
    expect(button).toHaveClass('db-menu-item-card--unavailable');
    expect(screen.getByText('Esgotado')).toBeInTheDocument();
  });
});
