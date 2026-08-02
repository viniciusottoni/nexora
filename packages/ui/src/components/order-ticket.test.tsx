// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { OrderTicket } from './order-ticket.js';

describe('OrderTicket', () => {
  it('lista os itens do pedido com modificadores destacados', () => {
    render(
      <OrderTicket
        code="42"
        where="Mesa 07"
        channel="DINE_IN"
        seconds={120}
        items={[{ qty: 1, name: 'Pizza G · Calabresa', modifiers: 'sem cebola · borda catupiry' }]}
      />,
    );

    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByText('Pizza G · Calabresa')).toBeInTheDocument();
    expect(screen.getByText('sem cebola · borda catupiry')).toBeInTheDocument();
    expect(screen.getByText('Salão')).toBeInTheDocument();
  });

  it('marca o cartão como atrasado quando os segundos excedem o limiar', () => {
    const { container } = render(
      <OrderTicket code="09" seconds={700} lateAt={600} items={[{ qty: 2, name: 'Refrigerante lata' }]} />,
    );

    expect(container.querySelector('.db-order-ticket--late')).not.toBeNull();
  });
});
