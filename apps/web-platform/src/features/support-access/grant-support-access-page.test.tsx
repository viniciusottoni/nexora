// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { GrantSupportAccessResponse } from '@nexora/contracts';
import { GrantSupportAccessPage } from './grant-support-access-page.js';
import type { SupportAccessApi } from './support-access-api.js';

afterEach(() => {
  cleanup();
});

const successResponse: GrantSupportAccessResponse = {
  token: 'abcd1234efgh5678ijkl',
  expiresAt: new Date('2026-08-04T12:00:00Z').toISOString(),
  notifiedCustomer: true,
};

function buildApi(overrides?: Partial<SupportAccessApi>): SupportAccessApi {
  return {
    grant: vi.fn(async () => successResponse),
    ...overrides,
  };
}

describe('GrantSupportAccessPage', () => {
  it('solicita acesso informando estabelecimento, motivo e duracao', async () => {
    const api = buildApi();
    render(<GrantSupportAccessPage api={api} />);

    fireEvent.change(screen.getByLabelText('Estabelecimento (id)'), {
      target: { value: 'tenant-123' },
    });
    fireEvent.change(screen.getByLabelText('Motivo'), {
      target: { value: 'Investigação de chamado #482' },
    });
    fireEvent.change(screen.getByLabelText('Duração'), { target: { value: '120' } });
    fireEvent.click(screen.getByRole('button', { name: 'Solicitar acesso' }));

    await waitFor(() =>
      expect(api.grant).toHaveBeenCalledWith('tenant-123', {
        reason: 'Investigação de chamado #482',
        durationMinutes: 120,
      }),
    );
  });

  it('exibe o token mascarado por padrao apos a concessao', async () => {
    const api = buildApi();
    render(<GrantSupportAccessPage api={api} />);

    fireEvent.change(screen.getByLabelText('Estabelecimento (id)'), {
      target: { value: 'tenant-123' },
    });
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Chamado #1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Solicitar acesso' }));

    await screen.findByText('Acesso concedido');
    expect(screen.queryByText(successResponse.token)).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Revelar token' }));
    expect(screen.getByText(successResponse.token)).toBeInTheDocument();
  });

  it('mostra erro quando a concessao falha', async () => {
    const api = buildApi({
      grant: vi.fn(async () => {
        throw new Error('Estabelecimento não encontrado.');
      }),
    });
    render(<GrantSupportAccessPage api={api} />);

    fireEvent.change(screen.getByLabelText('Estabelecimento (id)'), {
      target: { value: 'tenant-inexistente' },
    });
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Chamado #2' } });
    fireEvent.click(screen.getByRole('button', { name: 'Solicitar acesso' }));

    await screen.findByText('Estabelecimento não encontrado.');
  });
});
