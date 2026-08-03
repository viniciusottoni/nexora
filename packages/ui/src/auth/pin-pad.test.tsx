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

  it('permite digitar o PIN usando as teclas numéricas da linha superior do teclado', () => {
    render(<PinPad onSubmit={vi.fn()} />);
    for (const key of ['4', '8', '2', '1']) fireEvent.keyDown(document, { key });
    expect(screen.getByLabelText('PIN com 4 dígitos')).toBeInTheDocument();
  });

  it('permite digitar o PIN usando o teclado numérico (numpad)', () => {
    render(<PinPad onSubmit={vi.fn()} />);
    for (const key of ['4', '8', '2', '1'])
      fireEvent.keyDown(document, { key, code: `Numpad${key}` });
    expect(screen.getByLabelText('PIN com 4 dígitos')).toBeInTheDocument();
  });

  it('apaga o último dígito com a tecla Backspace do teclado', () => {
    render(<PinPad onSubmit={vi.fn()} />);
    fireEvent.keyDown(document, { key: '9' });
    expect(screen.getByLabelText('PIN com 1 dígitos')).toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Backspace' });
    expect(screen.getByLabelText('PIN vazio')).toBeInTheDocument();
  });

  it('confirma o PIN com a tecla Enter quando há ao menos 4 dígitos', () => {
    const submit = vi.fn();
    render(<PinPad onSubmit={submit} />);
    for (const key of ['4', '8', '2', '1']) fireEvent.keyDown(document, { key });
    fireEvent.keyDown(document, { key: 'Enter' });
    expect(submit).toHaveBeenCalledWith('4821');
  });

  it('não confirma com Enter quando o PIN tem menos de 4 dígitos', () => {
    const submit = vi.fn();
    render(<PinPad onSubmit={submit} />);
    fireEvent.keyDown(document, { key: '1' });
    fireEvent.keyDown(document, { key: 'Enter' });
    expect(submit).not.toHaveBeenCalled();
  });

  it('ignora teclas do teclado quando desabilitado', () => {
    render(<PinPad onSubmit={vi.fn()} disabled />);
    fireEvent.keyDown(document, { key: '9' });
    expect(screen.getByLabelText('PIN vazio')).toBeInTheDocument();
  });
});
