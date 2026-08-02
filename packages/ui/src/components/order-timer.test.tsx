// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { OrderTimer } from './order-timer.js';

describe('OrderTimer', () => {
  it('fica no estado ok e formata mm:ss quando ainda dentro do limiar de atenção', () => {
    render(<OrderTimer seconds={90} warnAt={300} lateAt={600} />);

    const timer = screen.getByText('1:30');
    expect(timer).toHaveAttribute('data-state', 'ok');
    expect(timer).not.toHaveClass('db-order-timer--late');
  });

  it('escalona para atenção ao cruzar `warnAt`, sem pulsar', () => {
    render(<OrderTimer seconds={305} warnAt={300} lateAt={600} />);

    const timer = screen.getByText('5:05');
    expect(timer).toHaveAttribute('data-state', 'warn');
    expect(timer).not.toHaveClass('db-order-timer--late');
  });

  it('escalona para atraso e pulsa ao cruzar `lateAt`', () => {
    render(<OrderTimer seconds={650} warnAt={300} lateAt={600} />);

    const timer = screen.getByText('10:50');
    expect(timer).toHaveAttribute('data-state', 'late');
    expect(timer).toHaveClass('db-order-timer--late');
  });
});
