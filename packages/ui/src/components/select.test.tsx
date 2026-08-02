// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Select } from './select.js';

describe('Select', () => {
  it('lista opções a partir de strings', () => {
    render(<Select aria-label="Praça" options={['Todas as praças', 'Montagem', 'Forno']} />);

    const select = screen.getByRole('combobox', { name: 'Praça' });
    expect(select).toBeInTheDocument();
    expect(screen.getAllByRole('option')).toHaveLength(3);
  });

  it('aplica a classe do tamanho lg no invólucro', () => {
    render(<Select aria-label="Tamanho" size="lg" options={['A']} />);

    const wrapper = screen.getByRole('combobox', { name: 'Tamanho' }).closest('span');
    expect(wrapper).toHaveClass('db-select--lg');
  });
});
