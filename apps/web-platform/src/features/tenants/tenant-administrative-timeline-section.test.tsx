// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});

import type {
  AdministrativeTimelineEntry,
  AdministrativeTimelineListResponse,
} from '@nexora/contracts';
import type { TenantAdministrativeTimelineApi } from './tenant-administrative-timeline-api.js';
import { TenantAdministrativeTimelineSection } from './tenant-administrative-timeline-section.js';

const TENANT_ID = '0198aabb-0001-7000-8000-000000000001';

const creationEntry: AdministrativeTimelineEntry = {
  type: 'CREATION',
  occurredAt: '2026-01-01T10:00:00Z',
  actor: null,
  origin: 'SYSTEM',
  reason: 'Provisionamento inicial do estabelecimento',
  correlationId: null,
  summary: 'Estabelecimento "Pizzaria Dona Betinha" criado.',
};

const statusEntry: AdministrativeTimelineEntry = {
  type: 'STATUS_CHANGED',
  occurredAt: '2026-02-01T10:00:00Z',
  actor: { id: '0198aabb-0002-7000-8000-000000000001', name: 'Ana Administradora' },
  origin: 'PLATFORM_ADMIN',
  reason: 'Divergência comercial identificada.',
  correlationId: '0198aabb-0003-7000-8000-000000000001',
  summary: 'Status alterado de Ativo para Suspenso.',
};

function buildResponse(
  data: AdministrativeTimelineEntry[],
  nextCursor: string | null = null,
): AdministrativeTimelineListResponse {
  return { data, nextCursor };
}

function buildApi(
  overrides: Partial<TenantAdministrativeTimelineApi> = {},
): TenantAdministrativeTimelineApi {
  return {
    list: vi.fn().mockResolvedValue(buildResponse([creationEntry, statusEntry])),
    ...overrides,
  };
}

describe('TenantAdministrativeTimelineSection', () => {
  it('mostra os fatos em ordem cronológica com ator, origem, motivo e correlationId (Gherkin "Linha do tempo administrativa")', async () => {
    const api = buildApi();
    render(<TenantAdministrativeTimelineSection tenantId={TENANT_ID} api={api} />);

    expect(
      await screen.findByText('Estabelecimento "Pizzaria Dona Betinha" criado.'),
    ).toBeInTheDocument();
    expect(screen.getByText('Status alterado de Ativo para Suspenso.')).toBeInTheDocument();
    expect(screen.getByText('Ana Administradora')).toBeInTheDocument();
    expect(screen.getByText('PLATFORM_ADMIN')).toBeInTheDocument();
    expect(screen.getByText('Divergência comercial identificada.')).toBeInTheDocument();
    expect(screen.getByText('0198aabb-0003-7000-8000-000000000001')).toBeInTheDocument();

    const items = screen.getAllByRole('listitem');
    // Mais recente primeiro na tela (§UX comum de timeline), mas ambos aparecem — nenhum é escondido.
    expect(items).toHaveLength(2);
  });

  it('ator ausente mostra "Sistema" (fato automático, sem administrador por trás)', async () => {
    const api = buildApi({ list: vi.fn().mockResolvedValue(buildResponse([creationEntry])) });
    render(<TenantAdministrativeTimelineSection tenantId={TENANT_ID} api={api} />);

    await screen.findByText('Estabelecimento "Pizzaria Dona Betinha" criado.');
    expect(screen.getByText('Sistema')).toBeInTheDocument();
  });

  it('sem fatos mostra estado vazio', async () => {
    const api = buildApi({ list: vi.fn().mockResolvedValue(buildResponse([])) });
    render(<TenantAdministrativeTimelineSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('Nenhum fato registrado')).toBeInTheDocument();
  });

  it('falha ao carregar mostra AlertBanner local (falha isolada por seção)', async () => {
    const api = buildApi({ list: vi.fn().mockRejectedValue(new Error('Serviço indisponível.')) });
    render(<TenantAdministrativeTimelineSection tenantId={TENANT_ID} api={api} />);

    expect(await screen.findByText('Serviço indisponível.')).toBeInTheDocument();
  });

  it('permite filtrar por período, ator e correlação conforme o escopo da US-157', async () => {
    const api = buildApi();
    render(<TenantAdministrativeTimelineSection tenantId={TENANT_ID} api={api} />);
    await screen.findByText('Estabelecimento "Pizzaria Dona Betinha" criado.');

    fireEvent.change(screen.getByLabelText('Data inicial'), { target: { value: '2026-01-01' } });
    fireEvent.change(screen.getByLabelText('Data final'), { target: { value: '2026-02-28' } });
    fireEvent.change(screen.getByLabelText('ID do ator'), {
      target: { value: '0198aabb-0002-7000-8000-000000000001' },
    });
    fireEvent.change(screen.getByLabelText('ID de correlação'), {
      target: { value: '0198aabb-0003-7000-8000-000000000001' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Aplicar filtros' }));

    await waitFor(() =>
      expect(api.list).toHaveBeenLastCalledWith(
        TENANT_ID,
        expect.objectContaining({
          from: '2026-01-01T00:00:00.000Z',
          to: '2026-02-28T23:59:59.999Z',
          actorId: '0198aabb-0002-7000-8000-000000000001',
          correlationId: '0198aabb-0003-7000-8000-000000000001',
        }),
      ),
    );
  });

  it('carrega a próxima página da linha do tempo usando o cursor retornado', async () => {
    const list = vi
      .fn()
      .mockResolvedValueOnce(buildResponse([creationEntry], 'cursor-seguinte'))
      .mockResolvedValueOnce(buildResponse([statusEntry]));
    const api = buildApi({ list });
    render(<TenantAdministrativeTimelineSection tenantId={TENANT_ID} api={api} />);

    fireEvent.click(await screen.findByRole('button', { name: 'Carregar mais fatos' }));

    await waitFor(() =>
      expect(list).toHaveBeenNthCalledWith(
        2,
        TENANT_ID,
        expect.objectContaining({ cursor: 'cursor-seguinte' }),
      ),
    );
    expect(screen.getAllByRole('listitem')).toHaveLength(2);
  });
});
