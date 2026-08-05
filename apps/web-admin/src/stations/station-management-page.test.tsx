// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { StationDto } from '@nexora/contracts';
import { StationManagementPage } from './station-management-page.js';

const stations: readonly StationDto[] = [
  {
    id: '0198aabb-2222-7000-8000-000000000001',
    code: 'OVEN',
    name: 'Forno',
    color: 'amber',
    capacitySlots: 5,
    isBottleneck: true,
    position: 1,
    isActive: true,
    linkedProductCount: 0,
  },
  {
    id: '0198aabb-2222-7000-8000-000000000002',
    code: 'ASSEMBLY',
    name: 'Montagem',
    color: 'blue',
    capacitySlots: null,
    isBottleneck: false,
    position: 2,
    isActive: true,
    linkedProductCount: 3,
  },
];

const noOpCreate = async () => stations[0]!;
const noOpUpdate = async () => stations[0]!;
const noOpDelete = async () => {};

describe('StationManagementPage', () => {
  it('lista as pracas cadastradas com gargalo e contagem de produtos', () => {
    render(
      <StationManagementPage
        stations={stations}
        onCreate={noOpCreate}
        onUpdate={noOpUpdate}
        onDelete={noOpDelete}
      />,
    );

    // "Forno" aparece na tabela e no título do editor (selecionado por padrão) — as duas ocorrências confirmam a listagem e a seleção inicial.
    expect(screen.getAllByText('Forno').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Montagem')).toBeInTheDocument();
    // "Gargalo" aparece no cabeçalho da coluna e no badge da linha do Forno.
    expect(screen.getAllByText('Gargalo').length).toBeGreaterThanOrEqual(2);
  });

  it('cria uma praca nova', async () => {
    const onCreate = vi.fn(noOpCreate);
    render(
      <StationManagementPage
        stations={stations}
        onCreate={onCreate}
        onUpdate={noOpUpdate}
        onDelete={noOpDelete}
      />,
    );

    fireEvent.click(screen.getAllByRole('button', { name: 'Nova praça' }).at(-1)!);
    const dialog = screen.getByRole('dialog', { name: 'Criar praça' });
    fireEvent.change(within(dialog).getByLabelText('Nome'), { target: { value: 'Bebidas' } });
    fireEvent.change(within(dialog).getByRole('textbox', { name: /C.digo/ }), {
      target: { value: 'BAR' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Criar praça' }));

    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith(
        expect.objectContaining({ code: 'BAR', name: 'Bebidas', isBottleneck: false }),
      ),
    );
  });

  it('avisa que a praca atual do gargalo sera desmarcada ao marcar outra', () => {
    render(
      <StationManagementPage
        stations={stations}
        onCreate={noOpCreate}
        onUpdate={noOpUpdate}
        onDelete={noOpDelete}
      />,
    );

    // Seleciona "Montagem" (não é o gargalo) e marca o switch — deve avisar sobre o Forno.
    fireEvent.click(screen.getAllByText('Montagem', { selector: 'td' }).at(-1)!);
    const selectedCardTitle = screen.getAllByText('Montagem', { selector: 'div.db-card__title' }).at(-1)!;
    const selectedCard = selectedCardTitle.closest('section');
    fireEvent.click(within(selectedCard!).getByRole('switch', { name: 'Marcar como gargalo' }));

    expect(within(selectedCard!).getByText(/deixará de ser o gargalo/)).toBeInTheDocument();
  });

  it('salva alteracoes da praca selecionada', async () => {
    const onUpdate = vi.fn(noOpUpdate);
    render(
      <StationManagementPage
        stations={stations}
        onCreate={noOpCreate}
        onUpdate={onUpdate}
        onDelete={noOpDelete}
      />,
    );

    fireEvent.change(screen.getAllByLabelText('Nome da praça').at(-1)!, {
      target: { value: 'Forno a lenha' },
    });
    fireEvent.click(screen.getAllByRole('button', { name: 'Salvar praça' }).at(-1)!);

    await waitFor(() =>
      expect(onUpdate).toHaveBeenCalledWith(
        stations[0]!.id,
        expect.objectContaining({ name: 'Forno a lenha' }),
      ),
    );
  });

  it('bloqueia exclusao quando ha produtos vinculados', () => {
    render(
      <StationManagementPage
        stations={stations}
        onCreate={noOpCreate}
        onUpdate={noOpUpdate}
        onDelete={noOpDelete}
      />,
    );

    fireEvent.click(screen.getAllByText('Montagem', { selector: 'td' }).at(-1)!);

    const selectedCardTitle = screen.getAllByText('Montagem', { selector: 'div.db-card__title' }).at(-1)!;
    const selectedCard = selectedCardTitle.closest('section');

    expect(
      within(selectedCard!).getByText(/3 produto\(s\) vinculado\(s\)/),
    ).toBeInTheDocument();
    expect(within(selectedCard!).queryByRole('button', { name: 'Excluir praça' })).not.toBeInTheDocument();
  });

  it('exclui a praca apos confirmacao quando nao ha produtos vinculados', async () => {
    const onDelete = vi.fn(noOpDelete);
    render(
      <StationManagementPage
        stations={stations}
        onCreate={noOpCreate}
        onUpdate={noOpUpdate}
        onDelete={onDelete}
      />,
    );

    fireEvent.click(screen.getAllByRole('button', { name: 'Excluir praça' }).at(-1)!);
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar exclusão' }));

    await waitFor(() => expect(onDelete).toHaveBeenCalledWith(stations[0]!.id));
  });
});
