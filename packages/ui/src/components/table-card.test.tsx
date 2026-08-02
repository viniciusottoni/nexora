// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { TableCard } from './table-card.js';

describe('TableCard', () => {
  it('mostra identificação, metadados e valor consumido', () => {
    render(
      <TableCard name="Mesa 12" status="OPEN" elapsed="42 min" guests={4} total="R$ 186,40" waiter="Jonas" />,
    );

    expect(screen.getByRole('button', { name: /Mesa 12/ })).toBeInTheDocument();
    expect(screen.getByText('42 min')).toBeInTheDocument();
    expect(screen.getByText('R$ 186,40')).toBeInTheDocument();
  });

  it('sinaliza mesa livre e mesa que exige atenção via classe', () => {
    const { rerender } = render(<TableCard name="Mesa 03" status="FREE" />);
    expect(screen.getByRole('button', { name: /Mesa 03/ })).toHaveClass('db-table-card--free');

    rerender(<TableCard name="Mesa 03" status="BILL_REQUESTED" attention />);
    expect(screen.getByRole('button', { name: /Mesa 03/ })).toHaveClass('db-table-card--attention');
  });

  it('executa ação de seleção da mesa', () => {
    const onClick = vi.fn();
    render(<TableCard name="Mesa 07" onClick={onClick} />);

    screen.getByRole('button', { name: /Mesa 07/ }).click();
    expect(onClick).toHaveBeenCalledOnce();
  });
});
