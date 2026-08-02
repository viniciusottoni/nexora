// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { PairDevicePage } from './pair-device-page.js';

describe('PairDevicePage', () => {
  it('oferece somente o código como campo e envia seis dígitos', async () => {
    const onPair = vi.fn(async () => undefined);
    render(
      <PairDevicePage
        profile={{ label: 'Caixa 1', kind: 'CASHIER', fingerprint: 'fp-local' }}
        onPair={onPair}
      />,
    );

    const input = screen.getByRole('textbox', { name: 'Código de pareamento' });
    expect(screen.getAllByRole('textbox')).toHaveLength(1);
    expect(input).toHaveAttribute('inputmode', 'numeric');
    expect(input).toHaveAttribute('maxlength', '6');

    fireEvent.change(input, { target: { value: '41a8302' } });
    fireEvent.click(screen.getByRole('button', { name: 'Autorizar este dispositivo' }));

    await waitFor(() =>
      expect(onPair).toHaveBeenCalledWith({
        code: '418302',
        label: 'Caixa 1',
        kind: 'CASHIER',
        fingerprint: 'fp-local',
      }),
    );
  });
});
