// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { PinScreen } from './pin-screen.js';

describe('PinScreen', () => {
  it('mostra login operacional em tela cheia com marca e propósito claros', () => {
    render(<PinScreen tenantName="Casa do Bairro" onSubmit={vi.fn()} />);
    expect(screen.getByRole('main')).toHaveClass('db-pin-screen');
    expect(screen.getByRole('heading', { name: 'Quem está operando?' })).toBeInTheDocument();
    expect(screen.getByText('Casa do Bairro')).toBeInTheDocument();
  });

  it('libera nova tentativa quando bloqueio termina', async () => {
    vi.useFakeTimers();
    render(<PinScreen tenantName="Casa" onSubmit={vi.fn()} retryAfterSeconds={1} />);
    expect(screen.getByRole('status')).toBeInTheDocument();
    await act(async () => vi.advanceTimersByTime(1_000));
    expect(screen.getByLabelText('PIN vazio')).toBeInTheDocument();
    vi.useRealTimers();
  });
});
