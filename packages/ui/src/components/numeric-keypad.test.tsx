// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { NumericKeypad } from './numeric-keypad.js';

describe('NumericKeypad', () => {
  it('adiciona dígitos, mostra marcadores de PIN e confirma sem teclado físico', () => {
    const onChange = vi.fn();
    const onSubmit = vi.fn();
    const { rerender } = render(
      <NumericKeypad value="482" onChange={onChange} onSubmit={onSubmit} length={4} showDots />,
    );

    expect(screen.getAllByText(/^[0-9]$/).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: '1' }));
    expect(onChange).toHaveBeenCalledWith('4821');

    rerender(<NumericKeypad value="4821" onChange={onChange} onSubmit={onSubmit} length={4} showDots />);
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
    expect(onSubmit).toHaveBeenCalledWith('4821');
  });

  it('apaga o último dígito e respeita o limite de tamanho', () => {
    const onChange = vi.fn();
    render(<NumericKeypad value="12" onChange={onChange} length={2} />);

    fireEvent.click(screen.getByRole('button', { name: '9' }));
    expect(onChange).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Apagar' }));
    expect(onChange).toHaveBeenCalledWith('1');
  });
});
