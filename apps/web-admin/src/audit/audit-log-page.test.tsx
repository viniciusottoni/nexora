// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  cleanup();
});
import type { AuditLogEntry, AuditLogFilters, AuditLogListResponse } from '@nexora/contracts';
import type { AuditApi } from './audit-api.js';
import { AuditLogPage } from './audit-log-page.js';

const entryDiscount: AuditLogEntry = {
  id: '0198aabb-1111-7000-8000-000000000001',
  action: 'DISCOUNT_APPLIED',
  summary: 'Desconto de 10% (R$ 19,80) aplicado',
  actor: { id: '0198aabb-1111-7000-8000-000000000002', name: 'Carlos' },
  authorizedBy: { id: '0198aabb-1111-7000-8000-000000000003', name: 'Ana' },
  device: { id: '0198aabb-1111-7000-8000-000000000004', label: 'Caixa 1' },
  occurredAt: '2026-08-01T20:00:00Z',
  target: { type: 'order', id: '0198aabb-1111-7000-8000-000000000005', label: 'Pedido A47' },
  before: { discount: 0 },
  after: { discount: 19.8 },
  reason: 'cortesia',
  traceId: null,
};

const entryPriceChange: AuditLogEntry = {
  id: '0198aabb-1111-7000-8000-000000000006',
  action: 'PRICE_CHANGED',
  summary: 'Preço da Pizza Grande alterado de R$ 45,00 para R$ 48,00',
  actor: { id: '0198aabb-1111-7000-8000-000000000002', name: 'Carlos' },
  authorizedBy: null,
  device: null,
  occurredAt: '2026-08-02T10:00:00Z',
  target: { type: 'price', id: '0198aabb-1111-7000-8000-000000000007', label: 'Pizza Grande' },
  before: null,
  after: null,
  reason: null,
  traceId: null,
};

function pageResponse(
  data: readonly AuditLogEntry[],
  meta: { nextCursor: string | null; hasMore: boolean },
): AuditLogListResponse {
  return { data: [...data], meta };
}

describe('AuditLogPage', () => {
  it('carrega e exibe o resumo em português de cada registro (US-091 §4)', async () => {
    const list = vi
      .fn()
      .mockResolvedValue(pageResponse([entryDiscount], { nextCursor: null, hasMore: false }));
    const auditApi = { list } as unknown as AuditApi;

    render(<AuditLogPage auditApi={auditApi} />);

    expect(await screen.findByText('Desconto de 10% (R$ 19,80) aplicado')).toBeInTheDocument();
    expect(screen.getByText('Carlos')).toBeInTheDocument();
    expect(screen.getByText('Ana')).toBeInTheDocument();
    expect(screen.getByText('Pedido A47')).toBeInTheDocument();
  });

  it('aplicar um filtro consulta a API novamente com os parâmetros escolhidos', async () => {
    const list = vi
      .fn()
      .mockResolvedValue(pageResponse([entryDiscount], { nextCursor: null, hasMore: false }));
    const auditApi = { list } as unknown as AuditApi;

    render(<AuditLogPage auditApi={auditApi} />);
    await screen.findByText('Desconto de 10% (R$ 19,80) aplicado');
    expect(list).toHaveBeenCalledTimes(1);

    fireEvent.change(screen.getByLabelText('Tipo de ação'), {
      target: { value: 'DISCOUNT_APPLIED' },
    });
    fireEvent.change(screen.getByLabelText('Operador (ID)'), {
      target: { value: '0198aabb-1111-7000-8000-000000000002' },
    });
    fireEvent.change(screen.getByLabelText('Valor mínimo'), { target: { value: '5000' } });
    fireEvent.click(screen.getByRole('button', { name: /Filtrar/ }));

    await waitFor(() => expect(list).toHaveBeenCalledTimes(2));
    const secondCallFilters = list.mock.calls[1]?.[0] as AuditLogFilters;
    expect(secondCallFilters).toMatchObject({
      action: 'DISCOUNT_APPLIED',
      actorId: '0198aabb-1111-7000-8000-000000000002',
      minAmount: '50.00',
      limit: 50,
    });
  });

  it('mostra "Carregar mais" enquanto hasMore é true, busca a próxima página por cursor e esconde o botão quando acaba', async () => {
    const list = vi.fn(async (filters?: AuditLogFilters) => {
      if (filters?.cursor === 'cursor-abc') {
        return pageResponse([entryPriceChange], { nextCursor: null, hasMore: false });
      }
      return pageResponse([entryDiscount], { nextCursor: 'cursor-abc', hasMore: true });
    });
    const auditApi = { list } as unknown as AuditApi;

    render(<AuditLogPage auditApi={auditApi} />);
    await screen.findByText('Desconto de 10% (R$ 19,80) aplicado');

    const loadMoreButton = screen.getByRole('button', { name: 'Carregar mais' });
    expect(loadMoreButton).toBeInTheDocument();

    fireEvent.click(loadMoreButton);

    await waitFor(() => expect(list).toHaveBeenCalledTimes(2));
    expect(list.mock.calls[1]?.[0]).toMatchObject({ cursor: 'cursor-abc' });

    // Modo append: o registro anterior continua na tela, o novo é concatenado ao final.
    expect(await screen.findByText(/Preço da Pizza Grande/)).toBeInTheDocument();
    expect(screen.getByText('Desconto de 10% (R$ 19,80) aplicado')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Carregar mais' })).not.toBeInTheDocument();
  });

  it('nunca exibe antes/depois como JSON bruto — sempre como lista de chave/valor legível', async () => {
    const list = vi
      .fn()
      .mockResolvedValue(pageResponse([entryDiscount], { nextCursor: null, hasMore: false }));
    const auditApi = { list } as unknown as AuditApi;

    render(<AuditLogPage auditApi={auditApi} />);
    await screen.findByText('Desconto de 10% (R$ 19,80) aplicado');

    // Expande a linha clicando nela.
    fireEvent.click(screen.getByText('Desconto de 10% (R$ 19,80) aplicado').closest('tr')!);

    expect(await screen.findByText('Antes')).toBeInTheDocument();
    expect(screen.getByText('Depois')).toBeInTheDocument();
    // A chave "discount" aparece traduzida em ambos os blocos (antes e depois).
    expect(screen.getAllByText('Desconto')).toHaveLength(2);
    // before.discount=0 e after.discount=19.8 (decimal em reais — ADR-017, nunca centavos-inteiro
    // neste backend) formatados como moeda, nunca "0"/"19.8" cru.
    expect(screen.getByText('R$ 0,00')).toBeInTheDocument();
    expect(screen.getByText('R$ 19,80')).toBeInTheDocument();

    // Nenhuma chave de JSON bruto (`{"`) em lugar nenhum da tela.
    expect(document.body.textContent).not.toMatch(/\{"/);
  });
});
