// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Icon } from './icon.js';

describe('Icon', () => {
  it('é decorativo por padrão (aria-hidden, sem role)', () => {
    const { container } = render(<Icon name="timer" />);

    const glyph = container.querySelector('.material-symbols-rounded');
    expect(glyph).toHaveAttribute('aria-hidden', 'true');
    expect(glyph).not.toHaveAttribute('role');
    expect(glyph).toHaveTextContent('timer');
  });

  it('ganha role="img" e nome acessível quando recebe label', () => {
    render(<Icon name="table_restaurant" label="Mesas" />);

    const glyph = screen.getByRole('img', { name: 'Mesas' });
    expect(glyph).not.toHaveAttribute('aria-hidden');
  });
});
