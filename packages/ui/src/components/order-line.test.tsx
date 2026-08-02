// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { OrderLine } from './order-line.js';

describe('OrderLine', () => {
  it('mostra quantidade, modificadores e observação do pedido', () => {
    render(
      <OrderLine qty={1} name="Pizza G · Calabresa" modifiers="borda catupiry" note="sem cebola" price="R$ 72,90" />,
    );

    expect(screen.getByText('1×')).toBeInTheDocument();
    expect(screen.getByText('borda catupiry')).toBeInTheDocument();
    expect(screen.getByText('sem cebola')).toBeInTheDocument();
    expect(screen.getByText('R$ 72,90')).toBeInTheDocument();
  });

  it('mantém o item cancelado visível, riscado, nunca removido (auditoria)', () => {
    render(<OrderLine qty={2} name="Suco de laranja" cancelled />);

    const name = screen.getByText('Suco de laranja');
    expect(name).toBeInTheDocument();
    expect(name.closest('.db-order-line')).toHaveClass('db-order-line--cancelled');
  });
});
