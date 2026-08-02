// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { ContrastAdvisor } from './contrast-advisor.js';

describe('ContrastAdvisor', () => {
  it('avisa a falha e oferece cor corrigida', () => {
    render(<ContrastAdvisor primary="#F5EFD8" surface="#FFFFFF" onPrimary="#FFFFFF" />);
    expect(screen.getByRole('alert')).toHaveTextContent('contraste WCAG AA');
    expect(screen.getAllByText(/Sugestão/)).not.toHaveLength(0);
  });

  it('confirma quando os pares alcançam WCAG AA', () => {
    render(<ContrastAdvisor primary="#174A3B" surface="#FFFFFF" onPrimary="#FFFFFF" />);
    expect(screen.getByText(/Contraste WCAG AA atendido/)).toBeInTheDocument();
  });
});
