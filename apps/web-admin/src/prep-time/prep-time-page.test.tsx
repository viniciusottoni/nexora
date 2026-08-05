// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { PrepTimePage, type PrepTimeVariantRow } from './prep-time-page.js';
import type { PrepTimeAnalysisResponse, StationDto } from '@nexora/contracts';

const variant: PrepTimeVariantRow = {
  variantId: '0198aabb-1111-7000-8000-000000000001',
  variantName: 'Grande',
  productId: '0198aabb-1111-7000-8000-000000000002',
  productName: 'Pizza Mussarela',
  prepMinutes: 12,
  warnMinutes: null,
  criticalMinutes: null,
  stationId: null,
  stationCode: null,
  stationName: null,
};

const stations: readonly StationDto[] = [
  {
    id: '0198aabb-1111-7000-8000-000000000009',
    code: 'FORNO',
    name: 'Forno',
    color: 'red',
    capacitySlots: null,
    isBottleneck: true,
    position: 0,
    isActive: true,
    linkedProductCount: 0,
  },
];

describe('PrepTimePage', () => {
  it('mostra "Sem praça" quando o produto não tem praça definida', () => {
    render(
      <PrepTimePage
        variants={[variant]}
        stations={stations}
        onUpdatePrepTime={vi.fn()}
        onReassignStation={vi.fn()}
        onLoadAnalysis={vi.fn()}
      />,
    );

    expect(screen.getByText('Pizza Mussarela')).toBeInTheDocument();
    expect(screen.getByText('Grande')).toBeInTheDocument();
    // "Sem praça" aparece duas vezes: na etiqueta colorida e como opção do seletor de praça.
    expect(screen.getAllByText('Sem praça')).toHaveLength(2);
  });

  it('salva tempo de preparo com limiares vazios como herança (null)', async () => {
    const onUpdatePrepTime = vi.fn(async () => undefined);
    render(
      <PrepTimePage
        variants={[variant]}
        stations={stations}
        onUpdatePrepTime={onUpdatePrepTime}
        onReassignStation={vi.fn()}
        onLoadAnalysis={vi.fn()}
      />,
    );

    fireEvent.change(screen.getAllByLabelText('Preparo (min)').at(-1)!, { target: { value: '14' } });
    fireEvent.click(screen.getAllByRole('button', { name: 'Salvar tempo de preparo' }).at(-1)!);

    await waitFor(() =>
      expect(onUpdatePrepTime).toHaveBeenCalledWith(variant.variantId, {
        prepMinutes: 14,
        warnMinutes: null,
        criticalMinutes: null,
      }),
    );
  });

  it('reatribui a praça e atualiza a etiqueta assim que o gestor escolhe uma opção', async () => {
    const onReassignStation = vi.fn(async () => undefined);
    render(
      <PrepTimePage
        variants={[variant]}
        stations={stations}
        onUpdatePrepTime={vi.fn()}
        onReassignStation={onReassignStation}
        onLoadAnalysis={vi.fn()}
      />,
    );

    const selectedCardTitle = screen.getAllByText('Pizza Mussarela', { selector: '.db-card__title' }).at(-1)!;
    const selectedCard = selectedCardTitle.closest('section');
    expect(selectedCard).not.toBeNull();

    fireEvent.change(within(selectedCard!).getByLabelText('Praça de produção'), {
      target: { value: stations[0]!.id },
    });

    await waitFor(() =>
      expect(onReassignStation).toHaveBeenCalledWith(variant.productId, stations[0]!.id),
    );
    expect(within(selectedCard!).getByText('Forno', { selector: '.prep-time-station-tag' })).toBeInTheDocument();
    expect(within(selectedCard!).queryByText('Sem praça', { selector: '.db-badge' })).not.toBeInTheDocument();
  });

  it('carrega e alterna o painel de comparativo estimado versus real', async () => {
    const analysis: PrepTimeAnalysisResponse = {
      variantId: variant.variantId,
      configuredMinutes: 12,
      effectiveWarnMinutes: 15,
      warnMinutesInherited: true,
      effectiveCriticalMinutes: 25,
      criticalMinutesInherited: true,
      actualAvgMinutes: 16.4,
      actualP90Minutes: null,
      sampleSize: 340,
      suggestion: 16,
      note: null,
    };
    const onLoadAnalysis = vi.fn(async () => analysis);
    render(
      <PrepTimePage
        variants={[variant]}
        stations={stations}
        onUpdatePrepTime={vi.fn()}
        onReassignStation={vi.fn()}
        onLoadAnalysis={onLoadAnalysis}
      />,
    );

    fireEvent.click(screen.getAllByRole('button', { name: 'Ver comparativo estimado x real' }).at(-1)!);

    expect(await screen.findByText(/considere ajustar para 16 min/i)).toBeInTheDocument();
    expect(screen.getByText('340 pedido(s)')).toBeInTheDocument();

    fireEvent.click(screen.getAllByRole('button', { name: 'Ocultar comparativo' }).at(-1)!);
    expect(screen.queryByText(/considere ajustar/i)).not.toBeInTheDocument();
  });

  it('exibe mensagem de erro quando a atualização falha', async () => {
    const onUpdatePrepTime = vi.fn(async () => {
      throw new Error('Falha simulada de rede.');
    });
    render(
      <PrepTimePage
        variants={[variant]}
        stations={stations}
        onUpdatePrepTime={onUpdatePrepTime}
        onReassignStation={vi.fn()}
        onLoadAnalysis={vi.fn()}
      />,
    );

    fireEvent.click(screen.getAllByRole('button', { name: 'Salvar tempo de preparo' }).at(-1)!);

    expect(await screen.findByText('Falha simulada de rede.')).toBeInTheDocument();
  });

  it('bloqueia limiares inválidos antes de chamar a API', async () => {
    const onUpdatePrepTime = vi.fn(async () => undefined);
    render(
      <PrepTimePage
        variants={[variant]}
        stations={stations}
        onUpdatePrepTime={onUpdatePrepTime}
        onReassignStation={vi.fn()}
        onLoadAnalysis={vi.fn()}
      />,
    );

    fireEvent.change(screen.getAllByLabelText('Atenção (min)').at(-1)!, { target: { value: '8' } });
    fireEvent.click(screen.getAllByRole('button', { name: 'Salvar tempo de preparo' }).at(-1)!);

    expect(onUpdatePrepTime).not.toHaveBeenCalled();
    expect(screen.getAllByRole('status').at(-1)).toHaveTextContent('atenção não pode ser menor');
  });
});
