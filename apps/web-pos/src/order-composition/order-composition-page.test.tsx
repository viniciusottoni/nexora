// @vitest-environment jsdom
// A tela usa a fila singleton real (`posOrderQueue`, ver order-composition-api.ts) quando nenhuma
// é injetada — só o teste do cenário de rede indisponível (US-034) chega a tocar o Dexie de
// verdade; os demais nunca falham por rede, então nunca enfileiram nada.
import 'fake-indexeddb/auto';
import '@testing-library/jest-dom/vitest';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { OrderCompositionPage } from './order-composition-page.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };
const sessionId = '0198aabb-1111-7000-8000-000000000009';

const tamanhoGroupId = '0198aabb-2222-7000-8000-000000000010';
const brotoModifierId = '0198aabb-2222-7000-8000-000000000011';
const pizzaGrandeVariantId = '0198aabb-3333-7000-8000-000000000012';
const mussarelaVariantId = '0198aabb-3333-7000-8000-000000000013';
const calabresaVariantId = '0198aabb-3333-7000-8000-000000000014';

function menuFixture() {
  return {
    tenantId: '0198aabb-1111-7000-8000-000000000001',
    tenantName: 'Dona Betinha',
    categories: [
      {
        id: '0198aabb-4444-7000-8000-000000000004',
        name: 'Pizzas',
        description: null,
        position: 0,
        products: [
          {
            id: '0198aabb-5555-7000-8000-000000000005',
            name: 'Pizza Margherita',
            description: 'Molho, mussarela, manjericão',
            ingredientsText: null,
            allergens: [],
            imageUrl: null,
            position: 0,
            fromPrice: '45.90',
          },
          {
            id: '0198aabb-6666-7000-8000-000000000006',
            name: 'Pizza Grande',
            description: 'Escolha o sabor',
            ingredientsText: null,
            allergens: [],
            imageUrl: null,
            position: 1,
            fromPrice: '52.00',
            variants: [{ id: pizzaGrandeVariantId, name: 'Pizza Grande', price: '52.00' }],
            modifierGroups: [
              {
                id: tamanhoGroupId,
                name: 'Tamanho',
                minSelect: 1,
                maxSelect: 1,
                isRequired: true,
                modifiers: [{ id: brotoModifierId, name: 'Broto', priceDelta: '0.00' }],
              },
            ],
            allowsFractions: true,
            maxFractions: 2,
            fractionFlavors: [
              { variantId: mussarelaVariantId, name: 'Mussarela', fractionGroup: 'salgadas', price: '45.00', available: true },
              { variantId: calabresaVariantId, name: 'Calabresa', fractionGroup: 'salgadas', price: '48.00', available: true },
            ],
          },
        ],
      },
    ],
  };
}

function jsonResponse(body: unknown, status = 200) {
  return { ok: status >= 200 && status < 300, status, json: () => Promise.resolve(body) };
}

async function openConfigurator(productName: string) {
  await screen.findByText(productName);
  fireEvent.click(screen.getByText(productName));
}

describe('OrderCompositionPage (US-030 §7/§10 — composição de pedido do garçom)', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('grupo de modificador obrigatorio pendente bloqueia a inclusao no pedido, com o erro certo', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse(menuFixture()));
    vi.stubGlobal('fetch', fetchMock);

    render(<OrderCompositionPage identity={identity} sessionId={sessionId} />);
    await openConfigurator('Pizza Grande');

    fireEvent.click(screen.getByRole('button', { name: 'Adicionar ao pedido' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Selecione uma opção do grupo "Tamanho" antes de adicionar.');
    // nada foi para a comanda — o "envio" fica bloqueado (US-030 §4, "nenhum pedido deve ser criado").
    fireEvent.click(screen.getByRole('button', { name: /Voltar/ }));
    expect(screen.getByText('Nenhum item adicionado ainda.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Confirmar pedido/ })).toBeDisabled();
  });

  it('libera a inclusao quando o grupo obrigatorio tem uma escolha, e o preco total atualiza a cada escolha', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse(menuFixture()));
    vi.stubGlobal('fetch', fetchMock);

    render(<OrderCompositionPage identity={identity} sessionId={sessionId} />);

    await openConfigurator('Pizza Margherita');
    fireEvent.click(screen.getByRole('button', { name: 'Adicionar ao pedido' }));
    expect(await screen.findByText(/Total: R\$\s*45,90/)).toBeInTheDocument();

    await openConfigurator('Pizza Grande');
    fireEvent.click(screen.getByRole('checkbox', { name: /Broto/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Adicionar ao pedido' }));

    // 45,90 (margherita) + 52,00 (pizza grande, grupo obrigatório atendido) = 97,90
    expect(await screen.findByText(/Total: R\$\s*97,90/)).toBeInTheDocument();
  });

  it('monta o meio a meio (fracoes) corretamente, com pesos iguais e sabores no carrinho', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse(menuFixture()));
    vi.stubGlobal('fetch', fetchMock);

    render(<OrderCompositionPage identity={identity} sessionId={sessionId} />);
    await openConfigurator('Pizza Grande');

    fireEvent.click(screen.getByRole('checkbox', { name: 'Meio a meio' }));
    fireEvent.click(screen.getByRole('checkbox', { name: /Mussarela/ }));
    fireEvent.click(screen.getByRole('checkbox', { name: /Calabresa/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Adicionar ao pedido' }));

    expect(await screen.findByText('Meio a meio: Mussarela / Calabresa')).toBeInTheDocument();
    // média de 45,00 e 48,00 = 46,50
    expect(screen.getByText(/Total: R\$\s*46,50/)).toBeInTheDocument();

    const postMock = vi.fn().mockResolvedValueOnce(
      jsonResponse(
        {
          order: {
            id: sessionId,
            shortCode: 'A47',
            status: 'PLACED',
            sessionId,
            channel: 'DineIn',
            total: '46.50',
            placedAt: '2026-08-03T20:00:00.000Z',
            items: [],
          },
          promisedAt: '2026-08-03T20:15:00.000Z',
          estimatedMinutes: 15,
        },
        201,
      ),
    );
    fetchMock.mockImplementationOnce(postMock);

    fireEvent.click(screen.getByRole('button', { name: /Confirmar pedido/ }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
    const [, postInit] = fetchMock.mock.calls[1] as [string, RequestInit];
    const body = JSON.parse(postInit.body as string) as {
      items: Array<{ fractions: Array<{ variantId: string; weight: number }> | null }>;
    };
    expect(body.items[0]?.fractions).toEqual([
      { variantId: mussarelaVariantId, weight: 0.5 },
      { variantId: calabresaVariantId, weight: 0.5 },
    ]);
  });

  it('envia a observacao livre do item no pedido confirmado', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse(menuFixture()));
    vi.stubGlobal('fetch', fetchMock);

    render(<OrderCompositionPage identity={identity} sessionId={sessionId} />);
    await openConfigurator('Pizza Margherita');

    fireEvent.change(screen.getByPlaceholderText('Ex.: bem assada, sem cebola'), {
      target: { value: 'bem assada, sem cebola' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Adicionar ao pedido' }));

    expect(await screen.findByText('bem assada, sem cebola')).toBeInTheDocument();

    fetchMock.mockResolvedValueOnce(
      jsonResponse(
        {
          order: {
            id: sessionId,
            shortCode: 'A48',
            status: 'PLACED',
            sessionId,
            channel: 'DineIn',
            total: '45.90',
            placedAt: '2026-08-03T20:00:00.000Z',
            items: [],
          },
          promisedAt: '2026-08-03T20:15:00.000Z',
          estimatedMinutes: 10,
        },
        201,
      ),
    );

    fireEvent.click(screen.getByRole('button', { name: /Confirmar pedido/ }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
    const [, postInit] = fetchMock.mock.calls[1] as [string, RequestInit];
    const body = JSON.parse(postInit.body as string) as { items: Array<{ notes: string | null }> };
    expect(body.items[0]?.notes).toBe('bem assada, sem cebola');

    // US-030 §10: "código curto do pedido exibido após a confirmação".
    expect(await screen.findByText('A48')).toBeInTheDocument();
  });

  it('mostra o erro da API mapeado para o item exato quando o servidor recusa o pedido, sem perder o carrinho', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse(menuFixture()));
    vi.stubGlobal('fetch', fetchMock);

    const { container } = render(<OrderCompositionPage identity={identity} sessionId={sessionId} />);
    await openConfigurator('Pizza Margherita');
    fireEvent.click(screen.getByRole('button', { name: 'Adicionar ao pedido' }));
    await waitFor(() => expect(container.querySelectorAll('.db-order-line__name')).toHaveLength(1));

    fetchMock.mockResolvedValueOnce(
      jsonResponse(
        {
          code: 'PRODUCT_UNAVAILABLE',
          detail: 'Produto indisponível.',
          meta: { itemIndex: 0 },
        },
        422,
      ),
    );

    fireEvent.click(screen.getByRole('button', { name: /Confirmar pedido/ }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Item 1 (Pizza Margherita) ficou indisponível — remova-o do pedido e tente novamente.',
    );
    // rollback claro: o carrinho continua lá, nada foi perdido.
    expect(container.querySelectorAll('.db-order-line__name')).toHaveLength(1);
  });

  it('US-034 §7/§10 — queda de LAN na confirmação: nunca mostra erro, avisa "recebido, sincronizando" e limpa o carrinho', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse(menuFixture()));
    vi.stubGlobal('fetch', fetchMock);
    const onOrderQueued = vi.fn();

    render(<OrderCompositionPage identity={identity} sessionId={sessionId} onOrderQueued={onOrderQueued} />);
    await openConfigurator('Pizza Margherita');
    fireEvent.click(screen.getByRole('button', { name: 'Adicionar ao pedido' }));
    expect(await screen.findByText(/Total: R\$\s*45,90/)).toBeInTheDocument();

    // A LAN cai bem no toque em "Confirmar pedido" — `fetch` nem chega a voltar uma Response.
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'));

    fireEvent.click(screen.getByRole('button', { name: /Confirmar pedido/ }));

    expect(await screen.findByText('Pedido recebido — sincronizando quando a conexão voltar.')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    // Envio otimista: o carrinho esvazia (a ação já está garantida na fila local, ADR-020).
    expect(screen.getByText('Nenhum item adicionado ainda.')).toBeInTheDocument();
    expect(onOrderQueued).toHaveBeenCalledTimes(1);
  });
});
