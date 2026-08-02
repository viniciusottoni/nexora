// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { QuantityStepper } from './quantity-stepper.js';

describe('QuantityStepper', () => {
  it('aumenta a quantidade respeitando o máximo', () => {
    const onChange = vi.fn();
    render(<QuantityStepper value={2} max={3} onChange={onChange} />);

    fireEvent.click(screen.getByRole('button', { name: 'Aumentar' }));
    expect(onChange).toHaveBeenCalledWith(3);
  });

  it('desabilita diminuir ao chegar no mínimo', () => {
    render(<QuantityStepper value={0} min={0} />);

    expect(screen.getByRole('button', { name: 'Diminuir' })).toBeDisabled();
  });
});
