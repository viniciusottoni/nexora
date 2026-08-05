// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { CategoryDto } from '@nexora/contracts';
import { CategoryManagementPage } from './category-management-page.js';

const categories: readonly CategoryDto[] = [
  {
    id: '0198aabb-3333-7000-8000-000000000001',
    name: 'Pizzas Salgadas',
    description: 'Sabores tradicionais',
    position: 0,
    isActive: true,
    productCount: 3,
  },
  {
    id: '0198aabb-3333-7000-8000-000000000002',
    name: 'Bebidas',
    description: null,
    position: 1,
    isActive: false,
    productCount: 0,
  },
];

const noOpCreate = async () => categories[0]!;
const noOpUpdate = async () => categories[0]!;
const noOpReorder = async () => {};
const noOpDeactivate = async () => {};

describe('CategoryManagementPage', () => {
  it('lista as categorias cadastradas com contagem de produtos e situacao', () => {
    render(
      <CategoryManagementPage
        categories={categories}
        onCreate={noOpCreate}
        onUpdate={noOpUpdate}
        onReorder={noOpReorder}
        onDeactivate={noOpDeactivate}
      />,
    );

    expect(screen.getAllByText('Pizzas Salgadas').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Bebidas')).toBeInTheDocument();
    expect(screen.getByText('Ativa')).toBeInTheDocument();
    expect(screen.getByText('Inativa')).toBeInTheDocument();
  });

  it('cria uma categoria nova', async () => {
    const onCreate = vi.fn(noOpCreate);
    render(
      <CategoryManagementPage
        categories={categories}
        onCreate={onCreate}
        onUpdate={noOpUpdate}
        onReorder={noOpReorder}
        onDeactivate={noOpDeactivate}
      />,
    );

    fireEvent.click(screen.getAllByRole('button', { name: 'Nova categoria' }).at(-1)!);
    const dialog = screen.getByRole('dialog', { name: 'Criar categoria' });
    fireEvent.change(within(dialog).getByLabelText('Nome'), { target: { value: 'Sobremesas' } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Criar categoria' }));

    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith(
        expect.objectContaining({ name: 'Sobremesas', position: categories.length }),
      ),
    );
  });

  it('reordena categorias movendo uma linha para baixo', async () => {
    const onReorder = vi.fn(noOpReorder);
    render(
      <CategoryManagementPage
        categories={categories}
        onCreate={noOpCreate}
        onUpdate={noOpUpdate}
        onReorder={onReorder}
        onDeactivate={noOpDeactivate}
      />,
    );

    fireEvent.click(
      screen.getAllByRole('button', { name: 'Mover Pizzas Salgadas para baixo' }).at(-1)!,
    );

    await waitFor(() =>
      expect(onReorder).toHaveBeenCalledWith([categories[1]!.id, categories[0]!.id]),
    );
  });

  it('salva alteracoes da categoria selecionada', async () => {
    const onUpdate = vi.fn(noOpUpdate);
    render(
      <CategoryManagementPage
        categories={categories}
        onCreate={noOpCreate}
        onUpdate={onUpdate}
        onReorder={noOpReorder}
        onDeactivate={noOpDeactivate}
      />,
    );

    fireEvent.change(screen.getAllByLabelText('Nome da categoria').at(-1)!, {
      target: { value: 'Pizzas Especiais' },
    });
    fireEvent.click(screen.getAllByRole('button', { name: 'Salvar categoria' }).at(-1)!);

    await waitFor(() =>
      expect(onUpdate).toHaveBeenCalledWith(
        categories[0]!.id,
        expect.objectContaining({ name: 'Pizzas Especiais' }),
      ),
    );
  });

  it('desativa e reativa a categoria selecionada sem apaga-la', async () => {
    const onDeactivate = vi.fn(noOpDeactivate);
    const onUpdate = vi.fn(noOpUpdate);
    render(
      <CategoryManagementPage
        categories={categories}
        onCreate={noOpCreate}
        onUpdate={onUpdate}
        onReorder={noOpReorder}
        onDeactivate={onDeactivate}
      />,
    );

    fireEvent.click(screen.getAllByRole('button', { name: 'Desativar categoria' }).at(-1)!);
    await waitFor(() => expect(onDeactivate).toHaveBeenCalledWith(categories[0]!.id));

    fireEvent.click(screen.getAllByText('Bebidas').at(-1)!);
    fireEvent.click(screen.getByRole('button', { name: 'Reativar categoria' }));
    await waitFor(() =>
      expect(onUpdate).toHaveBeenCalledWith(
        categories[1]!.id,
        expect.objectContaining({ isActive: true }),
      ),
    );
  });
});
