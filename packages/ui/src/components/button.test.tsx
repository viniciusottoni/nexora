// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { Button } from './button.js';

describe('Button', () => {
  it('mantém semântica, foco e estado ocupado', () => {
    render(<Button busy>Salvar</Button>);

    const button = screen.getByRole('button', { name: 'Salvar' });
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute('aria-busy', 'true');
  });

  it('executa ação disponível', () => {
    const onClick = vi.fn();
    render(<Button onClick={onClick}>Copiar</Button>);

    screen.getByRole('button', { name: 'Copiar' }).click();
    expect(onClick).toHaveBeenCalledOnce();
  });
});
