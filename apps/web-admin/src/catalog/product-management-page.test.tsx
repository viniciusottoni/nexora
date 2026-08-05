// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import {
  NO_STATION_SENTINEL,
  type CategoryDto,
  type ProductDto,
  type StationDto,
} from '@nexora/contracts';
import { ProductManagementPage } from './product-management-page.js';

vi.mock('./image-crop-dialog.js', () => ({
  ImageCropDialog: ({
    onConfirm,
  }: {
    onConfirm: (result: {
      blob: Blob;
      width: number;
      height: number;
      contentType: 'image/jpeg';
    }) => void;
  }) => (
    <button
      type="button"
      onClick={() =>
        onConfirm({
          blob: new Blob(['cropped'], { type: 'image/jpeg' }),
          width: 800,
          height: 800,
          contentType: 'image/jpeg',
        })
      }
    >
      Confirmar recorte de teste
    </button>
  ),
}));

const categories: readonly CategoryDto[] = [
  {
    id: '0198aabb-3333-7000-8000-000000000001',
    name: 'Pizzas Salgadas',
    description: null,
    position: 0,
    isActive: true,
    productCount: 2,
  },
  {
    id: '0198aabb-3333-7000-8000-000000000002',
    name: 'Bebidas',
    description: null,
    position: 1,
    isActive: true,
    productCount: 0,
  },
];

const stations: readonly StationDto[] = [
  {
    id: '0198aabb-2222-7000-8000-000000000001',
    code: 'OVEN',
    name: 'Forno',
    color: 'amber',
    capacitySlots: null,
    isBottleneck: false,
    position: 0,
    isActive: true,
    linkedProductCount: 1,
  },
];

const products: readonly ProductDto[] = [
  {
    id: '0198aabb-4444-7000-8000-000000000001',
    categoryId: categories[0]!.id,
    categoryName: 'Pizzas Salgadas',
    stationId: stations[0]!.id,
    stationName: 'Forno',
    name: 'Pizza Mussarela',
    description: 'Descrição',
    ingredientsText: 'molho, mussarela, orégano',
    allergens: ['glúten', 'lactose'],
    imageUrl: null,
    position: 0,
    isActive: true,
    isAvailable: true,
    allowsFractions: false,
    maxFractions: 1,
  },
  {
    id: '0198aabb-4444-7000-8000-000000000002',
    categoryId: categories[0]!.id,
    categoryName: 'Pizzas Salgadas',
    stationId: null,
    stationName: null,
    name: 'Pizza Calabresa',
    description: null,
    ingredientsText: null,
    allergens: [],
    imageUrl: null,
    position: 1,
    isActive: false,
    isAvailable: true,
    allowsFractions: false,
    maxFractions: 1,
  },
];

const noOpCreate = async () => products[0]!;
const noOpUpdate = async () => products[0]!;
const noOpReorder = async () => {};
const noOpActivate = async () => products[0]!;
const noOpDeactivate = async () => products[0]!;
const noOpUpload = async () => {};

function renderPage(overrides: Partial<React.ComponentProps<typeof ProductManagementPage>> = {}) {
  return render(
    <ProductManagementPage
      products={products}
      categories={categories}
      stations={stations}
      onCreate={noOpCreate}
      onUpdate={noOpUpdate}
      onReorder={noOpReorder}
      onActivate={noOpActivate}
      onDeactivate={noOpDeactivate}
      onUploadImage={noOpUpload}
      {...overrides}
    />,
  );
}

describe('ProductManagementPage', () => {
  it('lista os produtos com categoria, praça e situação', () => {
    const page = within(renderPage().container);

    expect(page.getAllByText('Pizza Mussarela').length).toBeGreaterThanOrEqual(2);
    expect(page.getByText('Pizza Calabresa')).toBeInTheDocument();
    expect(page.getAllByText('Forno').length).toBeGreaterThanOrEqual(1);
    expect(page.getByText('Ativo')).toBeInTheDocument();
    expect(page.getByText('Inativo')).toBeInTheDocument();
  });

  it('cria um produto com todos os dados do cadastro', async () => {
    const onCreate = vi.fn(noOpCreate);
    const page = within(renderPage({ onCreate }).container);

    fireEvent.click(page.getAllByRole('button', { name: 'Novo produto' }).at(-1)!);
    const dialog = await screen.findByRole('dialog', { name: 'Criar produto' });
    const modal = within(dialog);
    fireEvent.change(modal.getByLabelText('Nome'), {
      target: { value: 'Pizza Portuguesa' },
    });
    fireEvent.change(modal.getByLabelText('Praça de produção'), {
      target: { value: stations[0]!.id },
    });
    fireEvent.change(modal.getByLabelText('Descrição'), {
      target: { value: 'Pizza clássica' },
    });
    fireEvent.change(modal.getByLabelText('Ingredientes'), {
      target: { value: 'presunto, ovos' },
    });
    fireEvent.change(modal.getByLabelText('Alérgenos'), {
      target: { value: 'glúten, lactose' },
    });
    fireEvent.click(modal.getByLabelText('Permite frações'));
    fireEvent.change(modal.getByLabelText('Máximo de frações'), {
      target: { value: '4' },
    });
    fireEvent.click(modal.getByRole('button', { name: 'Criar produto' }));

    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'Pizza Portuguesa',
          categoryId: categories[0]!.id,
          stationId: stations[0]!.id,
          description: 'Pizza clássica',
          ingredientsText: 'presunto, ovos',
          allergens: ['glúten', 'lactose'],
          allowsFractions: true,
          maxFractions: 4,
          isActive: true,
        }),
      ),
    );
  });

  it('duplica o produto selecionado com um clique', async () => {
    const onCreate = vi.fn(noOpCreate);
    const page = within(renderPage({ onCreate }).container);

    fireEvent.click(page.getAllByRole('button', { name: 'Duplicar produto' }).at(-1)!);

    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'Pizza Mussarela (cópia)',
          categoryId: products[0]!.categoryId,
          stationId: products[0]!.stationId,
          ingredientsText: products[0]!.ingredientsText,
          allergens: products[0]!.allergens,
        }),
      ),
    );
  });

  it('envia a foto recortada depois de criar o produto', async () => {
    const onCreate = vi.fn(noOpCreate);
    const onUploadImage = vi.fn(noOpUpload);
    const page = within(renderPage({ onCreate, onUploadImage }).container);

    fireEvent.click(page.getAllByRole('button', { name: 'Novo produto' }).at(-1)!);
    const dialog = await screen.findByRole('dialog', { name: 'Criar produto' });
    const modal = within(dialog);
    fireEvent.change(modal.getByLabelText('Nome'), {
      target: { value: 'Pizza com foto' },
    });
    fireEvent.change(modal.getByLabelText('Foto do produto'), {
      target: { files: [new File(['photo'], 'photo.jpg', { type: 'image/jpeg' })] },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar recorte de teste' }));
    fireEvent.click(modal.getByRole('button', { name: 'Criar produto' }));

    await waitFor(() =>
      expect(onUploadImage).toHaveBeenCalledWith(products[0]!.id, expect.any(Blob), 'image/jpeg', {
        width: 800,
        height: 800,
      }),
    );
  });

  it('seleciona a primeira categoria quando os dados chegam depois da montagem', async () => {
    const onCreate = vi.fn(noOpCreate);
    const view = renderPage({ categories: [], onCreate });

    view.rerender(
      <ProductManagementPage
        products={products}
        categories={categories}
        stations={stations}
        onCreate={onCreate}
        onUpdate={noOpUpdate}
        onReorder={noOpReorder}
        onActivate={noOpActivate}
        onDeactivate={noOpDeactivate}
        onUploadImage={noOpUpload}
      />,
    );

    const page = within(view.container);
    fireEvent.click(page.getAllByRole('button', { name: 'Novo produto' }).at(-1)!);
    const dialog = await screen.findByRole('dialog', { name: 'Criar produto' });
    const modal = within(dialog);
    expect(modal.getByLabelText('Categoria')).toHaveValue(categories[0]!.id);
    fireEvent.change(modal.getByLabelText('Nome'), {
      target: { value: 'Produto carregado' },
    });
    fireEvent.click(modal.getByRole('button', { name: 'Criar produto' }));

    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith(
        expect.objectContaining({ categoryId: categories[0]!.id }),
      ),
    );
  });

  it('salva alteracoes de cadastro do produto selecionado', async () => {
    const onUpdate = vi.fn(noOpUpdate);
    const page = within(renderPage({ onUpdate }).container);

    fireEvent.change(page.getByLabelText('Nome do produto'), {
      target: { value: 'Pizza Mussarela Especial' },
    });
    fireEvent.click(page.getAllByRole('button', { name: 'Salvar produto' }).at(-1)!);

    await waitFor(() =>
      expect(onUpdate).toHaveBeenCalledWith(
        products[0]!.id,
        expect.objectContaining({ name: 'Pizza Mussarela Especial' }),
      ),
    );
  });

  it('desvincula a praça enviando o sentinela reservado', async () => {
    const onUpdate = vi.fn(noOpUpdate);
    const page = within(renderPage({ onUpdate }).container);

    fireEvent.change(page.getByLabelText('Praça de produção'), { target: { value: '' } });
    fireEvent.click(page.getAllByRole('button', { name: 'Salvar produto' }).at(-1)!);

    await waitFor(() =>
      expect(onUpdate).toHaveBeenCalledWith(
        products[0]!.id,
        expect.objectContaining({ stationId: NO_STATION_SENTINEL }),
      ),
    );
  });

  it('ativa e desativa o produto selecionado sem apaga-lo', async () => {
    const onDeactivate = vi.fn(noOpDeactivate);
    const onActivate = vi.fn(noOpActivate);
    const page = within(renderPage({ onDeactivate, onActivate }).container);

    fireEvent.click(page.getAllByRole('button', { name: 'Desativar produto' }).at(-1)!);
    await waitFor(() => expect(onDeactivate).toHaveBeenCalledWith(products[0]!.id));

    fireEvent.click(page.getByText('Pizza Calabresa'));
    fireEvent.click(page.getAllByRole('button', { name: 'Reativar produto' }).at(-1)!);
    await waitFor(() => expect(onActivate).toHaveBeenCalledWith(products[1]!.id));
  });

  it('so mostra controles de reordenacao quando uma categoria especifica esta filtrada', async () => {
    const onReorder = vi.fn(noOpReorder);
    const page = within(renderPage({ onReorder }).container);

    expect(page.queryByRole('button', { name: /Mover Pizza Mussarela/ })).not.toBeInTheDocument();

    fireEvent.change(page.getByLabelText('Filtrar por categoria'), {
      target: { value: categories[0]!.id },
    });

    fireEvent.click(page.getAllByRole('button', { name: 'Mover Pizza Mussarela para baixo' }).at(-1)!);

    await waitFor(() =>
      expect(onReorder).toHaveBeenCalledWith(categories[0]!.id, [products[1]!.id, products[0]!.id]),
    );
  });
});
