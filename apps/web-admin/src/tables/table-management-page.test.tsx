// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { AreaDto, TableDto } from '@nexora/contracts';
import { TableManagementPage, type TableManagementPageProps } from './table-management-page.js';

const areas: readonly AreaDto[] = [
  { id: '0198aabb-1111-7000-8000-000000000001', name: 'Salão', position: 0, active: true, tableCount: 2 },
  { id: '0198aabb-1111-7000-8000-000000000002', name: 'Varanda', position: 1, active: false, tableCount: 0 },
];

const tables: readonly TableDto[] = [
  {
    id: '0198aabb-2222-7000-8000-000000000001',
    areaId: areas[0]!.id,
    areaName: 'Salão',
    label: '1',
    seats: 4,
    status: 'FREE',
    active: true,
    sortOrder: 0,
  },
  {
    id: '0198aabb-2222-7000-8000-000000000002',
    areaId: areas[0]!.id,
    areaName: 'Salão',
    label: '2',
    seats: 4,
    status: 'OCCUPIED',
    active: true,
    sortOrder: 1,
  },
];

function noOp() {
  return async () => {};
}

function renderPage(overrides: Partial<TableManagementPageProps> = {}) {
  const handlers = {
    areas,
    tables,
    onCreateArea: vi.fn(noOp()),
    onDeactivateArea: vi.fn(noOp()),
    onActivateArea: vi.fn(noOp()),
    onDeleteArea: vi.fn(noOp()),
    onCreateTable: vi.fn(noOp()),
    onCreateTablesBulk: vi.fn(noOp()),
    onRotateToken: vi.fn(noOp()),
    onDeactivateTable: vi.fn(noOp()),
    onActivateTable: vi.fn(noOp()),
    onDeleteTable: vi.fn(noOp()),
    onExportQrCodesPdf: vi.fn(noOp()),
    ...overrides,
  };
  render(<TableManagementPage {...handlers} />);
  return handlers;
}

describe('TableManagementPage', () => {
  it('lista as mesas do ambiente selecionado com status canonico', () => {
    renderPage();

    expect(screen.getByText('Mesa')).toBeInTheDocument();
    expect(screen.getByText('Livre')).toBeInTheDocument();
    expect(screen.getByText('Ocupada')).toBeInTheDocument();
  });

  it('cria um novo ambiente', async () => {
    const handlers = renderPage();

    fireEvent.click(screen.getByRole('button', { name: 'Novo ambiente' }));
    const dialog = screen.getByRole('dialog', { name: 'Novo ambiente' });
    fireEvent.change(within(dialog).getByLabelText('Nome do ambiente'), { target: { value: 'Mezanino' } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Criar ambiente' }));

    await waitFor(() => expect(handlers.onCreateArea).toHaveBeenCalledWith('Mezanino'));
  });

  /** Cenário Gherkin "Criação em lote": a UI envia o intervalo exatamente como digitado. */
  it('cria mesas em lote com o intervalo informado', async () => {
    const handlers = renderPage();

    fireEvent.click(screen.getByRole('button', { name: 'Criar mesas em lote' }));
    const dialog = screen.getByRole('dialog', { name: 'Criar mesas em lote' });
    fireEvent.change(within(dialog).getByLabelText('De'), { target: { value: '1' } });
    fireEvent.change(within(dialog).getByLabelText('Até'), { target: { value: '20' } });
    fireEvent.click(within(dialog).getByRole('button', { name: /Criar mesas 1 a 20/ }));

    await waitFor(() =>
      expect(handlers.onCreateTablesBulk).toHaveBeenCalledWith({
        areaId: areas[0]!.id,
        from: 1,
        to: 20,
        seats: 4,
      }),
    );
  });

  /** US-020 §10: aviso explícito de que o QR impresso precisa ser trocado após a rotação. */
  it('exige confirmacao para rotacionar o token e avisa que o QR impresso precisa ser trocado', async () => {
    const handlers = renderPage();

    fireEvent.click(screen.getAllByRole('button', { name: 'Rotacionar token' })[0]!);
    const dialog = screen.getByRole('dialog', { name: /Rotacionar token da mesa/ });
    expect(within(dialog).getByText(/deixará de funcionar imediatamente/)).toBeInTheDocument();

    fireEvent.click(within(dialog).getByRole('button', { name: 'Sim, rotacionar token' }));

    await waitFor(() => expect(handlers.onRotateToken).toHaveBeenCalledWith(tables[0]!.id));
    expect(await screen.findByText(/parou de funcionar/)).toBeInTheDocument();
    expect(screen.getByText(/Exporte o PDF novamente/)).toBeInTheDocument();
  });

  it('exige confirmacao para excluir e explica a alternativa de desativacao', async () => {
    const handlers = renderPage();

    fireEvent.click(screen.getAllByRole('button', { name: 'Excluir' })[0]!);
    const dialog = screen.getByRole('dialog', { name: /Excluir mesa/ });
    expect(within(dialog).getByText(/desative-a em vez de excluir/)).toBeInTheDocument();

    fireEvent.click(within(dialog).getByRole('button', { name: 'Sim, excluir mesa' }));

    await waitFor(() => expect(handlers.onDeleteTable).toHaveBeenCalledWith(tables[0]!.id));
  });

  it('exporta os QR Codes filtrando pelo ambiente selecionado', async () => {
    const handlers = renderPage();

    fireEvent.click(screen.getByRole('button', { name: /^Salão/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Exportar QR Codes' }));

    await waitFor(() => expect(handlers.onExportQrCodesPdf).toHaveBeenCalledWith(areas[0]!.id));
  });

  it('mostra a mensagem de erro do backend quando uma acao falha', async () => {
    const handlers = renderPage({
      onDeleteTable: vi.fn(async () => {
        throw new Error('Esta mesa tem sessões no histórico e não pode ser excluída.');
      }),
    });

    fireEvent.click(screen.getAllByRole('button', { name: 'Excluir' })[0]!);
    fireEvent.click(screen.getByRole('button', { name: 'Sim, excluir mesa' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Esta mesa tem sessões no histórico e não pode ser excluída.',
    );
    expect(handlers.onDeleteTable).toHaveBeenCalled();
  });
});
