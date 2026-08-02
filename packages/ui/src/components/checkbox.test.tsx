// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Checkbox } from './checkbox.js';

describe('Checkbox', () => {
  it('marca um adicional do cardápio e exibe o preço', () => {
    render(<Checkbox label="Borda recheada de catupiry" price="+ R$ 8,00" />);

    const checkbox = screen.getByRole('checkbox', { name: 'Borda recheada de catupiry + R$ 8,00' });
    expect(checkbox).not.toBeChecked();

    fireEvent.click(checkbox);
    expect(checkbox).toBeChecked();
    expect(screen.getByText('+ R$ 8,00')).toBeInTheDocument();
  });

  it('funciona como escolha única (radio) com a classe de variante', () => {
    render(<Checkbox type="radio" name="massa" label="Massa fina" defaultChecked />);

    const radio = screen.getByRole('radio', { name: 'Massa fina' });
    expect(radio).toBeChecked();
    expect(radio.closest('label')).toHaveClass('db-checkbox--radio');
  });
});
