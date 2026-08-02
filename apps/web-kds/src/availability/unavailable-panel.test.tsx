// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { AvailabilityApi, AvailabilitySubscription } from './availability-api.js';
import { UnavailablePanel } from './unavailable-panel.js';

describe('UnavailablePanel', () => {
  it('carrega e exibe os produtos indisponiveis no KDS', async () => {
    const api = {
      listUnavailable: async () => ({
        items: [
          {
            productId: 'p1',
            productName: 'Pizza Calabresa',
            isAvailable: false,
            unavailableReason: 'Acabou o insumo',
            unavailableSince: '2026-08-02T20:00:00.000Z',
          },
        ],
      }),
    } as unknown as AvailabilityApi;
    const subscribeFn = () => ({ close: () => undefined }) satisfies AvailabilitySubscription;

    render(<UnavailablePanel api={api} subscribeFn={subscribeFn} />);

    expect(await screen.findByText('Pizza Calabresa')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Marcar disponível' })).toBeInTheDocument();
  });
});
