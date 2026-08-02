// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type {
  PreviewFractionPricingRequest,
  PreviewFractionPricingResponse,
} from '@nexora/contracts';
import {
  FractionBuilder,
  splitEqualWeights,
  type FractionFlavorOption,
  type FractionPricingApiLike,
} from './fraction-builder.js';

const MUSSARELA_G = 'v-mussarela-g';
const CALABRESA_G = 'v-calabresa-g';
const FRANGO_G = 'v-frango-g';
const PORTUGUESA_G = 'v-portuguesa-g';
const HAMBURGUER_G = 'v-hamburguer-g';
const FRANGO_M = 'v-frango-m';

const flavors: readonly FractionFlavorOption[] = [
  {
    variantId: MUSSARELA_G,
    productName: 'Mussarela',
    sizeCode: 'G',
    fractionGroup: 'PIZZA',
    available: true,
  },
  {
    variantId: CALABRESA_G,
    productName: 'Calabresa',
    sizeCode: 'G',
    fractionGroup: 'PIZZA',
    available: true,
  },
  {
    variantId: FRANGO_G,
    productName: 'Frango com Catupiry',
    sizeCode: 'G',
    fractionGroup: 'PIZZA',
    available: true,
  },
  {
    variantId: PORTUGUESA_G,
    productName: 'Portuguesa',
    sizeCode: 'G',
    fractionGroup: 'PIZZA',
    available: false,
    unavailableReason: 'Sem presunto no estoque',
  },
  {
    variantId: HAMBURGUER_G,
    productName: 'X-Salada',
    sizeCode: 'G',
    fractionGroup: 'HAMBURGUER',
    available: true,
  },
  // Nome deliberadamente distinto de FRANGO_G ("Frango com Catupiry") — os testes de troca de
  // tamanho precisam distinguir sem ambiguidade qual botão pertence a qual sizeCode.
  {
    variantId: FRANGO_M,
    productName: 'Marguerita',
    sizeCode: 'M',
    fractionGroup: 'PIZZA',
    available: true,
  },
];

function buildResponse(unitPrice: number, description: string): PreviewFractionPricingResponse {
  return { unitPrice, priceRule: 'HIGHEST', description, fractions: [] };
}

function makeApi(
  resolve: (req: PreviewFractionPricingRequest) => PreviewFractionPricingResponse,
): FractionPricingApiLike {
  return {
    preview: vi.fn(async (req: PreviewFractionPricingRequest) => resolve(req)),
  };
}

describe('FractionBuilder (US-013)', () => {
  it('seleciona o primeiro tamanho quando sabores chegam depois do carregamento inicial', () => {
    const api = makeApi(() => buildResponse(52, 'G · Mussarela / Calabresa'));
    const view = render(<FractionBuilder flavors={[]} maxFractions={2} api={api} />);

    view.rerender(<FractionBuilder flavors={flavors} maxFractions={2} api={api} />);

    expect(screen.getByRole('button', { name: 'Mussarela' })).toBeInTheDocument();
  });

  it('monta o item em duas etapas: tamanho primeiro, depois sabores compatíveis com o tamanho', () => {
    const api = makeApi(() => buildResponse(52, 'G · Mussarela / Calabresa'));
    render(<FractionBuilder flavors={flavors} maxFractions={2} api={api} />);

    // Etapa 1: tamanho — G já vem selecionado por padrão (primeiro da lista).
    expect(screen.getByRole('group', { name: 'Escolha o tamanho' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Mussarela' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Marguerita' })).not.toBeInTheDocument();

    // Trocar para tamanho M mostra só os sabores daquele tamanho.
    fireEvent.click(screen.getByRole('button', { name: 'M' }));

    expect(screen.getByRole('button', { name: 'Marguerita' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Mussarela' })).not.toBeInTheDocument();
  });

  it('exibe sabor indisponível bloqueado, com o motivo, e não permite selecioná-lo', () => {
    const api = makeApi(() => buildResponse(52, 'G · Mussarela / Calabresa'));
    render(<FractionBuilder flavors={flavors} maxFractions={2} api={api} />);

    const portuguesa = screen.getByRole('button', { name: /Portuguesa/ });
    expect(portuguesa).toBeDisabled();
    expect(screen.getByText('Sem presunto no estoque')).toBeInTheDocument();

    fireEvent.click(portuguesa);
    expect(portuguesa).toHaveAttribute('aria-pressed', 'false');
  });

  it('atualiza o preço a cada escolha, chamando o preview com pesos iguais que somam 1,0', async () => {
    const api = makeApi((req) => {
      const total = req.fractions.reduce((sum, f) => sum + f.weight, 0);
      expect(total).toBeCloseTo(1, 4);
      return buildResponse(52, 'G · Mussarela / Calabresa');
    });
    render(<FractionBuilder flavors={flavors} maxFractions={2} api={api} />);

    expect(
      screen.getByText('Escolha pelo menos dois sabores para ver o preço.'),
    ).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Mussarela' }));
    // Uma única fração ainda não dispara o preview (mínimo de duas, US-013 §3.1).
    expect(api.preview).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Calabresa' }));

    await waitFor(() => expect(api.preview).toHaveBeenCalledTimes(1));
    expect(await screen.findByText('R$ 52,00')).toBeInTheDocument();
    expect(screen.getByText('HIGHEST')).toBeInTheDocument();
    expect(screen.getByText('G · Mussarela / Calabresa')).toBeInTheDocument();

    // Troca de sabor (desmarca Calabresa, escolhe Frango) mantém duas frações -> dispara um NOVO preview.
    (api.preview as ReturnType<typeof vi.fn>).mockResolvedValueOnce(
      buildResponse(48.5, 'G · Mussarela / Frango com Catupiry'),
    );
    fireEvent.click(screen.getByRole('button', { name: 'Calabresa' })); // desmarca
    fireEvent.click(screen.getByRole('button', { name: 'Frango com Catupiry' }));

    await waitFor(() => expect(api.preview).toHaveBeenCalledTimes(2));
    expect(await screen.findByText('R$ 48,50')).toBeInTheDocument();
    expect(screen.getByText('G · Mussarela / Frango com Catupiry')).toBeInTheDocument();
  });

  it('bloqueia sabor de fraction_group diferente depois da primeira escolha', () => {
    const api = makeApi(() => buildResponse(52, 'G · Mussarela / Calabresa'));
    render(<FractionBuilder flavors={flavors} maxFractions={2} api={api} />);

    fireEvent.click(screen.getByRole('button', { name: 'Mussarela' }));

    // regex, não string exata: o botão bloqueado concatena nome + motivo no texto acessível.
    const hamburguer = screen.getByRole('button', { name: /X-Salada/ });
    expect(hamburguer).toBeDisabled();
    expect(hamburguer).toHaveAttribute('title', 'Não combina com o sabor já escolhido');

    fireEvent.click(hamburguer);
    expect(hamburguer).toHaveAttribute('aria-pressed', 'false');
  });

  it('impede escolher mais sabores que o limite (maxFractions)', async () => {
    const api = makeApi(() => buildResponse(52, 'G · Mussarela / Calabresa'));
    render(<FractionBuilder flavors={flavors} maxFractions={2} api={api} />);

    fireEvent.click(screen.getByRole('button', { name: 'Mussarela' }));
    fireEvent.click(screen.getByRole('button', { name: 'Calabresa' }));
    await waitFor(() => expect(api.preview).toHaveBeenCalledTimes(1));

    // regex, não string exata: o botão bloqueado concatena nome + motivo no texto acessível.
    const frango = screen.getByRole('button', { name: /Frango com Catupiry/ });
    expect(frango).toBeDisabled();
    expect(frango).toHaveAttribute('title', 'Limite de 2 sabores atingido');

    fireEvent.click(frango);
    expect(frango).toHaveAttribute('aria-pressed', 'false');
    // Nenhuma chamada extra de preview — a seleção não mudou.
    expect(api.preview).toHaveBeenCalledTimes(1);
  });

  it('mostra erro quando o preview falha, sem travar a interação', async () => {
    const api: FractionPricingApiLike = {
      preview: vi.fn().mockRejectedValue(new Error('Servidor fora do ar')),
    };
    render(<FractionBuilder flavors={flavors} maxFractions={2} api={api} />);

    fireEvent.click(screen.getByRole('button', { name: 'Mussarela' }));
    fireEvent.click(screen.getByRole('button', { name: 'Calabresa' }));

    expect(await screen.findByText('Servidor fora do ar')).toBeInTheDocument();
  });

  it('recalcula o preço quando o canal muda', async () => {
    const api = makeApi(() => buildResponse(52, 'G · Mussarela / Calabresa'));
    const view = render(
      <FractionBuilder flavors={flavors} maxFractions={2} api={api} channel="DineIn" />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Mussarela' }));
    fireEvent.click(screen.getByRole('button', { name: 'Calabresa' }));
    await waitFor(() => expect(api.preview).toHaveBeenCalledTimes(1));

    view.rerender(
      <FractionBuilder flavors={flavors} maxFractions={2} api={api} channel="Delivery" />,
    );

    await waitFor(() => expect(api.preview).toHaveBeenCalledTimes(2));
    expect(api.preview).toHaveBeenLastCalledWith(expect.objectContaining({ channel: 'Delivery' }));
  });

  it('splitEqualWeights soma exatamente 1,0 mesmo quando 1/N não é dízima finita', () => {
    expect(splitEqualWeights(2)).toEqual([0.5, 0.5]);

    const three = splitEqualWeights(3);
    expect(three.reduce((sum, w) => sum + w, 0)).toBeCloseTo(1, 8);
    expect(three).toHaveLength(3);

    const four = splitEqualWeights(4);
    expect(four).toEqual([0.25, 0.25, 0.25, 0.25]);
  });
});
