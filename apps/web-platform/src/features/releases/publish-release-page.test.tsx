// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});

import type { PublishReleaseResponse, ReleaseRolloutResponse } from '@nexora/contracts';
import type { ReleasesApi } from './releases-api.js';
import { PublishReleasePage } from './publish-release-page.js';

const publishedResponse: PublishReleaseResponse = {
  release: {
    id: '0198aabb-0004-7000-8000-000000000001',
    version: '1.5.0',
    rolloutPercent: 10,
    notes: 'Correção crítica no fechamento de comanda.',
    publishedAt: new Date().toISOString(),
    publishedBy: null,
  },
};

const rolloutResponse: ReleaseRolloutResponse = { total: 12, updated: 3, failed: 0, pending: 9 };

function buildApi(overrides: Partial<ReleasesApi> = {}): ReleasesApi {
  return {
    publish: vi.fn().mockResolvedValue(publishedResponse),
    rollout: vi.fn().mockResolvedValue(rolloutResponse),
    ...overrides,
  };
}

describe('PublishReleasePage', () => {
  it('publica uma versão nova e mostra o progresso da liberação (US-146 §7/§10)', async () => {
    const api = buildApi();
    render(<PublishReleasePage api={api} />);

    fireEvent.change(screen.getByLabelText('Versão'), { target: { value: '1.5.0' } });
    fireEvent.change(screen.getByLabelText('Percentual de liberação'), { target: { value: '10' } });
    fireEvent.click(screen.getByRole('button', { name: 'Publicar release' }));

    expect(await screen.findByText('3 de 12')).toBeInTheDocument();
    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getByText('9')).toBeInTheDocument();
    expect(api.publish).toHaveBeenCalledWith({ version: '1.5.0', rolloutPercent: 10, notes: null });
    expect(api.rollout).toHaveBeenCalledWith('1.5.0');
  });

  it('republicar com percentual menor mostra a mensagem de liberação que nunca reduz (US-146 §3.1)', async () => {
    const error = Object.assign(new Error('nunca reduz'), { code: 'RELEASE_ROLLOUT_CANNOT_DECREASE' });
    const api = buildApi({ publish: vi.fn().mockRejectedValue(error) });
    render(<PublishReleasePage api={api} />);

    fireEvent.change(screen.getByLabelText('Versão'), { target: { value: '1.5.0' } });
    fireEvent.click(screen.getByRole('button', { name: 'Publicar release' }));

    expect(await screen.findByText(/a liberação gradual nunca reduz/i)).toBeInTheDocument();
  });

  it('mostra alerta de rollback quando há falhas na versão consultada', async () => {
    const api = buildApi({
      rollout: vi.fn().mockResolvedValue({ total: 10, updated: 6, failed: 2, pending: 2 }),
    });
    render(<PublishReleasePage api={api} />);

    fireEvent.change(screen.getByLabelText('Versão para consultar'), { target: { value: '1.5.0' } });
    fireEvent.click(screen.getByRole('button', { name: 'Consultar' }));

    expect(await screen.findByText('Rollbacks nesta versão')).toBeInTheDocument();
    expect(api.rollout).toHaveBeenCalledWith('1.5.0');
  });

  it('erro ao publicar mostra a mensagem de erro', async () => {
    const api = buildApi({ publish: vi.fn().mockRejectedValue(new Error('Falha de rede')) });
    render(<PublishReleasePage api={api} />);

    fireEvent.change(screen.getByLabelText('Versão'), { target: { value: '1.5.0' } });
    fireEvent.click(screen.getByRole('button', { name: 'Publicar release' }));

    expect(await screen.findByText('Falha de rede')).toBeInTheDocument();
  });
});
