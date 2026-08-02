// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { TopBar } from './top-bar.js';

describe('TopBar', () => {
  it('mostra título, subtítulo e o slot da direita', () => {
    render(<TopBar title="Caixa · Terminal 1" subtitle="Turno aberto às 18:02" right={<span>Sincronizado</span>} />);

    expect(screen.getByText('Caixa · Terminal 1')).toBeInTheDocument();
    expect(screen.getByText('Turno aberto às 18:02')).toBeInTheDocument();
    expect(screen.getByText('Sincronizado')).toBeInTheDocument();
  });

  it('aplica a classe da variante', () => {
    render(<TopBar title="Painel" variant="brand" />);
    const header = screen.getByText('Painel').closest('header');
    expect(header?.className).toContain('db-topbar--brand');
  });
});
