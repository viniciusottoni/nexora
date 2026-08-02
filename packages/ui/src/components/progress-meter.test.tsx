// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ProgressMeter } from './progress-meter.js';

describe('ProgressMeter', () => {
  it('expõe o valor realizado via role="meter" para leitores de tela', () => {
    render(<ProgressMeter label="Aderência ao prazo" value={82} display="82%" target={85} tone="warning" caption="meta 85%" />);

    const meter = screen.getByRole('meter');
    expect(meter).toHaveAttribute('aria-valuenow', '82');
    expect(meter).toHaveAttribute('aria-valuemax', '100');
    expect(screen.getByText('82%')).toBeInTheDocument();
    expect(screen.getByText('meta 85%')).toBeInTheDocument();
  });

  it('satura o preenchimento em 100% quando o valor excede o máximo', () => {
    render(<ProgressMeter value={140} max={100} tone="danger" />);

    const fill = document.querySelector('.db-meter__fill');
    expect(fill).toHaveClass('db-meter__fill--danger');
    expect(fill).toHaveStyle({ width: '100%' });
  });
});
