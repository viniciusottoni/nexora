// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { DevicePairingScreen, pairingFailureMessage } from './device-pairing-screen.js';

describe('DevicePairingScreen', () => {
  it('pede somente o cÃ³digo de pareamento', () => {
    render(<DevicePairingScreen kind="CASHIER" defaultLabel="Caixa" onPaired={vi.fn()} />);

    expect(screen.getAllByRole('textbox')).toHaveLength(1);
    expect(screen.getByRole('textbox', { name: /pareamento/i })).toHaveAttribute('maxlength', '6');
    expect(screen.getByText('Caixa')).toBeInTheDocument();
  });
  it('diferencia codigo consumido de falha tecnica de idempotencia', async () => {
    const consumed = await pairingFailureMessage(
      new Response(JSON.stringify({ code: 'DEVICE_PAIRING_CODE_CONSUMED' }), { status: 403 }),
    );
    const inProgress = await pairingFailureMessage(
      new Response(JSON.stringify({ code: 'REQUEST_IN_PROGRESS' }), { status: 409 }),
    );

    expect(consumed).toBe(
      'C\u00f3digo de pareamento j\u00e1 utilizado. Gere um novo c\u00f3digo.',
    );
    expect(inProgress).toContain('ainda est\u00e1 sendo processada');
  });
});
