// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { DevicePairingScreen } from './device-pairing-screen.js';

describe('DevicePairingScreen', () => {
  it('pede somente o cÃ³digo de pareamento', () => {
    render(<DevicePairingScreen kind="CASHIER" defaultLabel="Caixa" onPaired={vi.fn()} />);

    expect(screen.getAllByRole('textbox')).toHaveLength(1);
    expect(screen.getByRole('textbox', { name: /pareamento/i })).toHaveAttribute('maxlength', '6');
    expect(screen.getByText('Caixa')).toBeInTheDocument();
  });
});
