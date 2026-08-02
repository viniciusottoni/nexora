// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StatusPill } from './status-pill.js';

describe('StatusPill', () => {
  it('usa o rótulo e a cor canônicos do estado', () => {
    render(<StatusPill status="IN_OVEN" />);

    const pill = screen.getByText('No forno');
    expect(pill).toHaveClass('db-status-pill');
    expect(pill).not.toHaveClass('db-status-pill--lg');
    expect(pill).toHaveStyle({ color: 'var(--nx-warning-600)' });
  });

  it('permite sobrescrever o rótulo sem perder a cor do estado', () => {
    render(<StatusPill status="PAID" label="Pagamento confirmado" />);

    expect(screen.getByText('Pagamento confirmado')).toBeInTheDocument();
    expect(screen.queryByText('Pago')).not.toBeInTheDocument();
  });

  it('marca o ponto como vivo apenas quando `live` exige ação imediata', () => {
    render(<StatusPill status="BILL_REQUESTED" size="lg" live />);

    const pill = screen.getByText('Conta pedida');
    expect(pill).toHaveClass('db-status-pill--lg');
    expect(pill).toHaveClass('db-status-pill--live');
  });
});
