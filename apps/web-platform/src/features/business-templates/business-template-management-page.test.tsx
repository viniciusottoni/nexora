// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, waitFor, fireEvent } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { BusinessTemplateDetailResponse, BusinessTemplateSummary } from '@nexora/contracts';
import type { BusinessTemplatesApi } from './business-templates-api.js';
import { BusinessTemplateManagementPage } from './business-template-management-page.js';

afterEach(() => {
  cleanup();
});

const summaries: BusinessTemplateSummary[] = [
  { code: 'PIZZERIA', name: 'Pizzaria', version: 3 },
  { code: 'HAMBURGUERIA', name: 'Hamburgueria', version: 1 },
];

function buildDetail(overrides: Partial<BusinessTemplateDetailResponse> = {}): BusinessTemplateDetailResponse {
  return {
    code: 'PIZZERIA',
    name: 'Pizzaria',
    version: 3,
    isActive: true,
    configJson: '{"operation":{"bottleneck":{"resource":"OVEN"}}}',
    seedsJson: '{"stations":[]}',
    createdAt: '2026-08-01T09:00:00.000Z',
    updatedAt: '2026-08-01T09:00:00.000Z',
    ...overrides,
  };
}

describe('BusinessTemplateManagementPage', () => {
  it('lista os modelos e carrega o detalhe do primeiro automaticamente', async () => {
    const api = {
      list: vi.fn(async () => summaries),
      get: vi.fn(async () => buildDetail()),
      update: vi.fn(),
    } as unknown as BusinessTemplatesApi;

    render(<BusinessTemplateManagementPage api={api} />);

    expect(await screen.findByText('Hamburgueria')).toBeInTheDocument();
    expect(api.get).toHaveBeenCalledWith('PIZZERIA');
    expect(await screen.findByDisplayValue('Pizzaria')).toBeInTheDocument();
  });

  it('salvar envia o JSON editado e mostra a versão nova, sem afetar tenants já provisionados', async () => {
    const update = vi.fn(async () => buildDetail({ version: 4, name: 'Pizzaria Clássica' }));
    const api = {
      list: vi.fn(async () => summaries),
      get: vi.fn(async () => buildDetail()),
      update,
    } as unknown as BusinessTemplatesApi;

    render(<BusinessTemplateManagementPage api={api} />);

    await screen.findByDisplayValue('Pizzaria');
    fireEvent.click(screen.getByRole('button', { name: 'Salvar modelo' }));

    await waitFor(() => expect(update).toHaveBeenCalledWith('PIZZERIA', {
      name: 'Pizzaria',
      configJson: '{"operation":{"bottleneck":{"resource":"OVEN"}}}',
      seedsJson: '{"stations":[]}',
    }));
    expect(await screen.findByText(/agora na versão 4/)).toBeInTheDocument();
  });

  it('JSON inválido desabilita o botão de salvar', async () => {
    const api = {
      list: vi.fn(async () => summaries),
      get: vi.fn(async () => buildDetail()),
      update: vi.fn(),
    } as unknown as BusinessTemplatesApi;

    render(<BusinessTemplateManagementPage api={api} />);

    const configField = await screen.findByLabelText('Configuração (JSON)');
    fireEvent.change(configField, { target: { value: '{invalido' } });

    expect(screen.getByRole('button', { name: 'Salvar modelo' })).toBeDisabled();
    expect(api.update).not.toHaveBeenCalled();
  });
});
