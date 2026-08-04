// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});
import type { TenantThresholds } from '@nexora/contracts';
import type { ThresholdsApi } from './thresholds-api.js';
import { ThresholdConfigPage } from './threshold-config-page.js';

const thresholds: TenantThresholds = {
  orderWarnMinutes: 12,
  orderCriticalMinutes: 18,
  itemInWindowMinutes: 5,
  tableIdleMinutes: 30,
  cashDivergenceAlert: '5.00',
  cmvDivergencePercent: 3,
  syncDelayMinutes: 5,
  dineInPromiseMinutes: 25,
  deliveryPromiseMinutes: 40,
  avgTimeAboveTargetPercent: 20,
  cancellationCountThreshold: 3,
  cancellationWindowMinutes: 60,
  discountAboveThresholdPercent: 15,
  discountWindowMinutes: 60,
};

describe('ThresholdConfigPage', () => {
  it('carrega e exibe os valores atuais dos limiares (US-080 §10)', async () => {
    const get = vi.fn().mockResolvedValue(thresholds);
    const thresholdsApi = { get, update: vi.fn() } as unknown as ThresholdsApi;

    render(<ThresholdConfigPage thresholdsApi={thresholdsApi} />);

    expect(await screen.findByLabelText('Pedido atrasado (crítico)')).toHaveValue(18);
    expect(screen.getByLabelText('Aviso de pedido demorado')).toHaveValue(12);
    expect(screen.getByLabelText('Divergência de caixa')).toHaveValue('5,00');
    expect(get).toHaveBeenCalledTimes(1);
  });

  it('editar um campo e salvar dispara PATCH só com os campos alterados', async () => {
    const get = vi.fn().mockResolvedValue(thresholds);
    const update = vi.fn().mockResolvedValue({ ...thresholds, orderCriticalMinutes: 22 });
    const thresholdsApi = { get, update } as unknown as ThresholdsApi;

    render(<ThresholdConfigPage thresholdsApi={thresholdsApi} />);
    await screen.findByLabelText('Pedido atrasado (crítico)');

    fireEvent.change(screen.getByLabelText('Pedido atrasado (crítico)'), {
      target: { value: '22' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Salvar limiares' }));

    await waitFor(() => expect(update).toHaveBeenCalledTimes(1));
    expect(update).toHaveBeenCalledWith({ orderCriticalMinutes: 22 });
  });

  it('editar o campo monetário de divergência de caixa envia o valor como string decimal', async () => {
    const get = vi.fn().mockResolvedValue(thresholds);
    const update = vi.fn().mockResolvedValue({ ...thresholds, cashDivergenceAlert: '10.00' });
    const thresholdsApi = { get, update } as unknown as ThresholdsApi;

    render(<ThresholdConfigPage thresholdsApi={thresholdsApi} />);
    await screen.findByLabelText('Divergência de caixa');

    fireEvent.change(screen.getByLabelText('Divergência de caixa'), {
      target: { value: '1000' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Salvar limiares' }));

    await waitFor(() => expect(update).toHaveBeenCalledTimes(1));
    expect(update).toHaveBeenCalledWith({ cashDivergenceAlert: '10.00' });
  });

  it('erro de carregamento mostra estado de erro', async () => {
    const get = vi.fn().mockRejectedValue(new Error('Falha de rede'));
    const thresholdsApi = { get, update: vi.fn() } as unknown as ThresholdsApi;

    render(<ThresholdConfigPage thresholdsApi={thresholdsApi} />);

    expect(await screen.findByText('Falha de rede')).toBeInTheDocument();
    expect(screen.queryByLabelText('Pedido atrasado (crítico)')).not.toBeInTheDocument();
  });

  it('sem alterações pendentes o botão de salvar fica desabilitado', async () => {
    const get = vi.fn().mockResolvedValue(thresholds);
    const thresholdsApi = { get, update: vi.fn() } as unknown as ThresholdsApi;

    render(<ThresholdConfigPage thresholdsApi={thresholdsApi} />);
    await screen.findByLabelText('Pedido atrasado (crítico)');

    expect(screen.getByRole('button', { name: 'Salvar limiares' })).toBeDisabled();
  });
});
