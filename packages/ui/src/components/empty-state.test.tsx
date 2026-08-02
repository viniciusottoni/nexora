// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { EmptyState } from './empty-state.js';

describe('EmptyState', () => {
  it('diz o que fazer, não só que está vazio', () => {
    render(
      <EmptyState icon="restaurant" title="Fila vazia" action={<button type="button">Ver histórico do turno</button>}>
        Nenhum item aguardando produção nesta praça.
      </EmptyState>,
    );

    expect(screen.getByText('Fila vazia')).toHaveClass('db-empty-state__title');
    expect(screen.getByText('Nenhum item aguardando produção nesta praça.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Ver histórico do turno' })).toBeInTheDocument();
  });

  it('usa o ícone padrão "inbox" quando nenhum é informado', () => {
    render(<EmptyState title="Nada por aqui" />);

    expect(screen.getByText('inbox')).toBeInTheDocument();
  });
});
