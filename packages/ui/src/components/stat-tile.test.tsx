// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StatTile } from './stat-tile.js';

describe('StatTile', () => {
  it('exibe comparativo junto do valor — número nunca fica solto', () => {
    render(<StatTile label="Faturamento hoje" value="R$ 4.180" delta="+12,4%" comparison="vs. mesma terça" />);

    expect(screen.getByText('R$ 4.180')).toBeInTheDocument();
    expect(screen.getByText('+12,4%')).toBeInTheDocument();
    expect(screen.getByText('vs. mesma terça')).toBeInTheDocument();
  });

  it('infere direção da variação a partir do sinal quando deltaDirection não é informado', () => {
    render(<StatTile label="Pedidos em atraso" value="3" delta="-8%" target="≤ 10 min" />);

    const delta = screen.getByText('-8%');
    expect(delta.className).toContain('db-stat__delta--down');
    expect(screen.getByText(/meta/)).toHaveTextContent('meta ≤ 10 min');
  });

  it('aplica variante pulse para a faixa de tempo real', () => {
    render(<StatTile variant="pulse" size="lg" label="Pedidos em atraso" value="3" />);

    expect(screen.getByText('Pedidos em atraso').parentElement).toHaveClass('db-stat--pulse');
  });
});
