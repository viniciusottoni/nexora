// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { PinPad } from './pin-pad.js';

describe('PinPad', () => {
  it('permite autenticar com quatro a seis dígitos usando botões grandes', () => {
    const submit = vi.fn();
    render(<PinPad onSubmit={submit} />);
    for (const digit of ['4', '8', '2', '1'])
      fireEvent.click(screen.getByRole('button', { name: digit }));
    expect(screen.getByLabelText('PIN com 4 dígitos')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    expect(submit).toHaveBeenCalledWith('4821');
  });

  it('não exibe PIN e permite apagar sem teclado físico', () => {
    render(<PinPad onSubmit={vi.fn()} />);
    fireEvent.click(screen.getByRole('button', { name: '9' }));
    expect(screen.getByLabelText('PIN com 1 dígitos')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Apagar último dígito' }));
    expect(screen.getByLabelText('PIN vazio')).toBeInTheDocument();
  });
});
