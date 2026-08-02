// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { SegmentedControl } from './segmented-control.js';

describe('SegmentedControl', () => {
  it('marca a opção selecionada com aria-pressed e notifica a troca', () => {
    const onChange = vi.fn();
    render(<SegmentedControl options={['Hoje', '7 dias', 'Mês']} value="Hoje" onChange={onChange} />);

    expect(screen.getByRole('button', { name: 'Hoje' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Mês' })).toHaveAttribute('aria-pressed', 'false');

    screen.getByRole('button', { name: 'Mês' }).click();
    expect(onChange).toHaveBeenCalledWith('Mês');
  });

  it('aplica os modificadores de tamanho e bloco', () => {
    render(<SegmentedControl options={['A', 'B']} size="lg" block />);
    const group = screen.getByRole('group');
    expect(group.className).toContain('db-segmented-control--lg');
    expect(group.className).toContain('db-segmented-control--block');
  });
});
