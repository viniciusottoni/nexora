// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { IconButton } from './icon-button.js';

describe('IconButton', () => {
  it('usa label como nome acessível e title', () => {
    render(<IconButton icon="close" label="Fechar" />);

    const button = screen.getByRole('button', { name: 'Fechar' });
    expect(button).toHaveAttribute('title', 'Fechar');
    expect(button).toHaveClass('db-icon-button--md', 'db-icon-button--ghost');
  });

  it('mostra o contador do badge e executa ação', () => {
    const onClick = vi.fn();
    render(<IconButton icon="notifications" label="Alertas" badge={3} onClick={onClick} />);

    const button = screen.getByRole('button', { name: 'Alertas' });
    expect(button).toHaveTextContent('3');
    button.click();
    expect(onClick).toHaveBeenCalledOnce();
  });
});
