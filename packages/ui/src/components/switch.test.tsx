// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Switch } from './switch.js';

describe('Switch', () => {
  it('liga com efeito imediato e mostra a descrição do parâmetro', () => {
    render(<Switch label="Bloquear venda sem insumo" description="RF-EST-12 · reflete em todos os canais" defaultChecked />);

    const toggle = screen.getByRole('switch');
    expect(toggle).toBeChecked();
    expect(screen.getByText('RF-EST-12 · reflete em todos os canais')).toBeInTheDocument();

    fireEvent.click(toggle);
    expect(toggle).not.toBeChecked();
  });

  it('não renderiza rótulo quando nenhum é informado', () => {
    render(<Switch aria-label="Ativo" />);

    expect(screen.getByRole('switch')).toBeInTheDocument();
    expect(screen.queryByText('undefined')).not.toBeInTheDocument();
  });
});
