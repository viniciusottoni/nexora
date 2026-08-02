// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { validateModifierSelection, type ModifierGroup } from '@nexora/contracts';
import {
  ModifierGroupManagementPage,
  moneyToCents,
  normalizeMoneyInput,
} from './modifier-group-management-page.js';

const tamanhoGroup: ModifierGroup = {
  id: '0198aabb-2222-7000-8000-000000000001',
  name: 'Tamanho',
  minSelect: 1,
  maxSelect: 1,
  isRequired: true,
  sortOrder: 0,
  productIds: [],
  modifiers: [
    {
      id: '0198aabb-2222-7000-8000-000000000011',
      groupId: '0198aabb-2222-7000-8000-000000000001',
      name: 'Pequena',
      priceDelta: '0.00',
      ingredientId: null,
      quantity: null,
      isAvailable: true,
      sortOrder: 0,
    },
    {
      id: '0198aabb-2222-7000-8000-000000000012',
      groupId: '0198aabb-2222-7000-8000-000000000001',
      name: 'Grande',
      priceDelta: '10.00',
      ingredientId: null,
      quantity: null,
      isAvailable: true,
      sortOrder: 1,
    },
  ],
};

const adicionaisGroup: ModifierGroup = {
  id: '0198aabb-2222-7000-8000-000000000002',
  name: 'Adicionais',
  minSelect: 0,
  maxSelect: 3,
  isRequired: false,
  sortOrder: 1,
  productIds: ['0198aabb-3333-7000-8000-000000000001'],
  modifiers: [
    {
      id: '0198aabb-2222-7000-8000-000000000021',
      groupId: '0198aabb-2222-7000-8000-000000000002',
      name: 'Bacon',
      priceDelta: '5.00',
      ingredientId: null,
      quantity: null,
      isAvailable: true,
      sortOrder: 0,
    },
    {
      id: '0198aabb-2222-7000-8000-000000000022',
      groupId: '0198aabb-2222-7000-8000-000000000002',
      name: 'Borda Catupiry',
      priceDelta: '8.00',
      ingredientId: null,
      quantity: null,
      isAvailable: true,
      sortOrder: 1,
    },
    {
      id: '0198aabb-2222-7000-8000-000000000023',
      groupId: '0198aabb-2222-7000-8000-000000000002',
      name: 'Cheddar',
      priceDelta: '4.00',
      ingredientId: null,
      quantity: null,
      isAvailable: true,
      sortOrder: 2,
    },
    {
      id: '0198aabb-2222-7000-8000-000000000024',
      groupId: '0198aabb-2222-7000-8000-000000000002',
      name: 'Sem cebola',
      priceDelta: '0.00',
      ingredientId: null,
      quantity: null,
      isAvailable: true,
      sortOrder: 3,
    },
  ],
};

const noOpAsync = async () => undefined as never;

function renderPage(groups: readonly ModifierGroup[] = [tamanhoGroup, adicionaisGroup]) {
  return render(
    <ModifierGroupManagementPage
      groups={groups}
      onCreateGroup={noOpAsync}
      onUpdateGroup={noOpAsync}
      onDeleteGroup={noOpAsync}
      onCreateModifier={noOpAsync}
      onUpdateModifierPrice={noOpAsync}
      onSetModifierAvailability={noOpAsync}
      onLinkToProduct={noOpAsync}
      onUnlinkFromProduct={noOpAsync}
    />,
  );
}

describe('validateModifierSelection (função pura, US-012 §10)', () => {
  it('permite selecionar mais enquanto abaixo do máximo e sinaliza mínimo não atingido', () => {
    const result = validateModifierSelection({ minSelect: 1, maxSelect: 3 }, 0);

    expect(result.canSelectMore).toBe(true);
    expect(result.meetsMinimum).toBe(false);
    expect(result.remaining).toBe(3);
  });

  it('bloqueia a quarta seleção quando o máximo é três (cenário Gherkin "Limite máximo de seleção")', () => {
    const result = validateModifierSelection({ minSelect: 0, maxSelect: 3 }, 3);

    expect(result.canSelectMore).toBe(false);
    expect(result.remaining).toBe(0);
  });

  it('considera o mínimo atingido assim que a contagem alcança o valor exigido', () => {
    const result = validateModifierSelection({ minSelect: 1, maxSelect: 1 }, 1);

    expect(result.meetsMinimum).toBe(true);
    expect(result.canSelectMore).toBe(false);
  });

  it('nunca reporta restante negativo mesmo com contagem acima do máximo (estado defensivo)', () => {
    const result = validateModifierSelection({ minSelect: 0, maxSelect: 2 }, 5);

    expect(result.remaining).toBe(0);
  });
});

describe('ModifierGroupManagementPage', () => {
  it('normaliza entrada monetária em centavos sem usar ponto flutuante no cálculo', () => {
    expect(normalizeMoneyInput('9,5')).toBe('9.50');
    expect(moneyToCents('9999999999.99')).toBe(999999999999);
    expect(moneyToCents('-3.50')).toBe(-350);
    expect(moneyToCents('1.001')).toBeNull();
  });

  it('destaca grupo obrigatório pendente antes de o cliente tentar avançar (cenário Gherkin "Modificador obrigatório")', () => {
    renderPage();

    expect(screen.getByText(/Escolha pendente: este grupo é obrigatório/)).toBeInTheDocument();
    expect(screen.getByText(/Escolha até 1 · 0 selecionado/)).toBeInTheDocument();
  });

  it('some com o aviso obrigatório assim que a seleção mínima é atingida', () => {
    renderPage();

    fireEvent.click(screen.getByRole('checkbox', { name: /^Grande/ }));

    expect(screen.queryByText(/Escolha pendente/)).not.toBeInTheDocument();
    expect(screen.getByText(/1 selecionado/)).toBeInTheDocument();
  });

  it('troca diretamente a opção de seleção única sem exigir desmarcar a anterior', () => {
    renderPage();

    fireEvent.click(screen.getByRole('checkbox', { name: /^Pequena/ }));
    fireEvent.click(screen.getByRole('checkbox', { name: /^Grande/ }));

    expect(screen.getByRole('checkbox', { name: /^Pequena/ })).not.toBeChecked();
    expect(screen.getByRole('checkbox', { name: /^Grande/ })).toBeChecked();
    expect(screen.queryByText(/Limite de 1 opções atingido/)).not.toBeInTheDocument();
  });

  it('bloqueia a quarta seleção do grupo "Adicionais" (máximo 3) e mantém as três primeiras (cenário Gherkin "Limite máximo de seleção")', () => {
    renderPage();

    fireEvent.click(screen.getByRole('button', { name: /Adicionais/ }));

    fireEvent.click(screen.getByRole('checkbox', { name: /^Bacon/ }));
    fireEvent.click(screen.getByRole('checkbox', { name: /^Borda Catupiry/ }));
    fireEvent.click(screen.getByRole('checkbox', { name: /^Cheddar/ }));
    fireEvent.click(screen.getByRole('checkbox', { name: /^Sem cebola/ }));

    expect(screen.getByText(/Limite de 3 opções atingido/)).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: /^Bacon/ })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: /^Borda Catupiry/ })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: /^Cheddar/ })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: /^Sem cebola/ })).not.toBeChecked();
  });

  /** Cenário Gherkin "Preço do adicional somado": pizza de R$ 45,00 + Borda Catupiry de R$ 8,00 = R$ 53,00. */
  it('soma o adicional ao preço base do item', () => {
    renderPage();
    fireEvent.click(screen.getByRole('button', { name: /Adicionais/ }));

    const baseInput = screen.getByLabelText('Preço base do item (R$)');
    fireEvent.change(baseInput, { target: { value: '45.00' } });
    fireEvent.click(screen.getByRole('checkbox', { name: /^Borda Catupiry/ }));

    expect(screen.getByText(/Total do item: R\$\s?53,00/)).toBeInTheDocument();
  });

  /** Cenário Gherkin "Remoção sem custo": não muda o preço e aparece em destaque no cartão do KDS simulado. */
  it('remoção sem custo não altera o preço e aparece em destaque no cartão do KDS', () => {
    renderPage();
    fireEvent.click(screen.getByRole('button', { name: /Adicionais/ }));

    const baseInput = screen.getByLabelText('Preço base do item (R$)');
    fireEvent.change(baseInput, { target: { value: '45.00' } });
    fireEvent.click(screen.getByRole('checkbox', { name: /^Sem cebola/ }));

    expect(screen.getByText(/Total do item: R\$\s?45,00/)).toBeInTheDocument();
    expect(screen.getByText('SEM CEBOLA')).toBeInTheDocument();
  });

  it('permite atualizar o preço de modificador existente', async () => {
    const onUpdateModifierPrice = vi.fn(async () => adicionaisGroup.modifiers[1]!);
    render(
      <ModifierGroupManagementPage
        groups={[adicionaisGroup]}
        onCreateGroup={noOpAsync}
        onUpdateGroup={noOpAsync}
        onDeleteGroup={noOpAsync}
        onCreateModifier={noOpAsync}
        onUpdateModifierPrice={onUpdateModifierPrice}
        onSetModifierAvailability={noOpAsync}
        onLinkToProduct={noOpAsync}
        onUnlinkFromProduct={noOpAsync}
      />,
    );

    fireEvent.change(screen.getByLabelText('Preço de Borda Catupiry'), {
      target: { value: '9.50' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Salvar preço de Borda Catupiry' }));

    await waitFor(() =>
      expect(onUpdateModifierPrice).toHaveBeenCalledWith(
        adicionaisGroup.id,
        adicionaisGroup.modifiers[1]!.id,
        '9.50',
      ),
    );
  });

  it('ao marcar grupo como obrigatório ajusta mínimo para um antes de criar', async () => {
    const onCreateGroup = vi.fn(async () => tamanhoGroup);
    render(
      <ModifierGroupManagementPage
        groups={[]}
        onCreateGroup={onCreateGroup}
        onUpdateGroup={noOpAsync}
        onDeleteGroup={noOpAsync}
        onCreateModifier={noOpAsync}
        onUpdateModifierPrice={noOpAsync}
        onSetModifierAvailability={noOpAsync}
        onLinkToProduct={noOpAsync}
        onUnlinkFromProduct={noOpAsync}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Novo grupo' }));
    fireEvent.change(screen.getByLabelText('Nome'), { target: { value: 'Tamanho' } });
    fireEvent.click(screen.getByRole('switch', { name: /^Obrigatório/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Criar grupo' }));

    await waitFor(() =>
      expect(onCreateGroup).toHaveBeenCalledWith(
        expect.objectContaining({ minSelect: 1, isRequired: true }),
      ),
    );
  });

  it('cria grupo pelo diálogo "Novo grupo"', async () => {
    const onCreateGroup = vi.fn(async () => tamanhoGroup);
    render(
      <ModifierGroupManagementPage
        groups={[]}
        onCreateGroup={onCreateGroup}
        onUpdateGroup={noOpAsync}
        onDeleteGroup={noOpAsync}
        onCreateModifier={noOpAsync}
        onUpdateModifierPrice={noOpAsync}
        onSetModifierAvailability={noOpAsync}
        onLinkToProduct={noOpAsync}
        onUnlinkFromProduct={noOpAsync}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Novo grupo' }));
    fireEvent.change(screen.getByLabelText('Nome'), { target: { value: 'Ponto da massa' } });
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Criar grupo' }));
    });

    await waitFor(() =>
      expect(onCreateGroup).toHaveBeenCalledWith({
        name: 'Ponto da massa',
        minSelect: 0,
        maxSelect: 1,
        isRequired: false,
        sortOrder: 0,
      }),
    );
  });
});
