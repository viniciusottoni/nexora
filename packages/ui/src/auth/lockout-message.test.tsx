// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { LockoutMessage } from './lockout-message.js';

describe('LockoutMessage', () => {
  afterEach(() => vi.useRealTimers());
  it('informa tempo restante sem revelar se o PIN existe', async () => {
    vi.useFakeTimers();
    render(<LockoutMessage retryAfterSeconds={900} />);
    expect(screen.getByRole('status')).toHaveTextContent('15:00');
    expect(screen.getByRole('status')).not.toHaveTextContent('usuário');
    await act(async () => vi.advanceTimersByTime(1_000));
    expect(screen.getByRole('status')).toHaveTextContent('14:59');
  });
});
