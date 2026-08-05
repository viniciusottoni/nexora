// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { NumericKeypad } from './numeric-keypad.js';

describe('NumericKeypad (US-041)', () => {
  afterEach(() => {
    cleanup();
  });

  it('digita pelo toque na grade e envia ao apertar Enter — zero digitação livre', () => {
    const onSubmit = vi.fn();
    render(<NumericKeypad onSubmit={onSubmit} onSubmitBatch={vi.fn()} />);

    fireEvent.click(screen.getByRole('button', { name: '4' }));
    fireEvent.click(screen.getByRole('button', { name: '7' }));
    expect(screen.getByTestId('kds-keypad-display')).toHaveValue('47');

    fireEvent.click(screen.getByRole('button', { name: 'Enter' }));

    expect(onSubmit).toHaveBeenCalledWith('47');
    expect(screen.getByTestId('kds-keypad-display')).toHaveValue('');
  });

  it('aceita dígitos do teclado numérico físico e Enter físico', () => {
    const onSubmit = vi.fn();
    render(<NumericKeypad onSubmit={onSubmit} onSubmitBatch={vi.fn()} />);

    const display = screen.getByTestId('kds-keypad-display');
    fireEvent.keyDown(display, { key: '1' });
    fireEvent.keyDown(display, { key: '2' });
    fireEvent.keyDown(display, { key: 'Enter' });

    expect(onSubmit).toHaveBeenCalledWith('12');
  });

  it('ignora texto livre digitado por qualquer via que não seja dígito 0-9', () => {
    const onSubmit = vi.fn();
    render(<NumericKeypad onSubmit={onSubmit} onSubmitBatch={vi.fn()} />);

    const display = screen.getByTestId('kds-keypad-display');
    for (const key of ['a', 'b', 'c', '9', 'x', 'y', 'z']) {
      fireEvent.keyDown(display, { key });
    }
    fireEvent.keyDown(display, { key: 'Enter' });

    expect(onSubmit).toHaveBeenCalledWith('9');
  });

  it('botão Lote envia via onSubmitBatch, distinto do Enter comum', () => {
    const onSubmit = vi.fn();
    const onSubmitBatch = vi.fn();
    render(<NumericKeypad onSubmit={onSubmit} onSubmitBatch={onSubmitBatch} />);

    fireEvent.click(screen.getByRole('button', { name: '4' }));
    fireEvent.click(screen.getByRole('button', { name: 'Lote' }));

    expect(onSubmitBatch).toHaveBeenCalledWith('4');
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('erro limpa o campo automaticamente e nunca trava a tela (US-041 §10)', () => {
    const { rerender } = render(
      <NumericKeypad onSubmit={vi.fn()} onSubmitBatch={vi.fn()} error="Código não encontrado." />,
    );

    expect(screen.getByTestId('kds-keypad-error')).toHaveTextContent('Código não encontrado.');
    expect(screen.getByTestId('kds-keypad-display')).toHaveValue('');

    rerender(<NumericKeypad onSubmit={vi.fn()} onSubmitBatch={vi.fn()} />);
    expect(screen.queryByTestId('kds-keypad-error')).not.toBeInTheDocument();
  });

  it('desfazer habilitado só quando undoAvailable=true, acionável sem mouse (tecla "-")', () => {
    const onUndo = vi.fn();
    render(<NumericKeypad onSubmit={vi.fn()} onSubmitBatch={vi.fn()} onUndo={onUndo} undoAvailable />);

    expect(screen.getByTestId('kds-keypad-undo')).toBeEnabled();

    fireEvent.keyDown(screen.getByTestId('kds-keypad-display'), { key: '-' });

    expect(onUndo).toHaveBeenCalledOnce();
  });

  it('desfazer desabilitado quando a janela expirou', () => {
    render(<NumericKeypad onSubmit={vi.fn()} onSubmitBatch={vi.fn()} onUndo={vi.fn()} undoAvailable={false} />);

    expect(screen.getByTestId('kds-keypad-undo')).toBeDisabled();
  });

  it('desabilita toda a grade enquanto uma operação está em andamento (disabled=true)', () => {
    render(<NumericKeypad onSubmit={vi.fn()} onSubmitBatch={vi.fn()} disabled />);

    expect(screen.getByRole('button', { name: '4' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Enter' })).toBeDisabled();
  });
});
