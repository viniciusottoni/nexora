// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Badge } from './badge.js';

describe('Badge', () => {
  it('aplica tone e size como classes', () => {
    render(<Badge tone="danger" size="sm">Atrasado</Badge>);

    const badge = screen.getByText('Atrasado');
    expect(badge).toHaveClass('db-badge', 'db-badge--danger', 'db-badge--sm');
  });

  it('renderiza o ícone quando informado e usa canto reto com square', () => {
    render(
      <Badge tone="info" icon="delivery_dining" square>
        Delivery
      </Badge>,
    );

    const badge = screen.getByText('Delivery');
    expect(badge).toHaveClass('db-badge--square');
    expect(badge.querySelector('.material-symbols-rounded')).toHaveTextContent('delivery_dining');
  });
});
