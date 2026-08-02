// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { VariantDto } from '@nexora/contracts';
import { VariantPriceEditor } from './variant-price-editor.js';

const pequena: VariantDto = {
  id: '0198aabb-5555-7000-8000-000000000001',
  productId: '0198aabb-4444-7000-8000-000000000001',
  name: 'Pequena',
  sku: null,
  sizeCode: 'P',
  prepMinutes: 10,
  isDefault: true,
  isActive: true,
  currentPrice: '35.00',
  currentPriceChannel: 'DineIn',
};

const grande: VariantDto = {
  id: '0198aabb-5555-7000-8000-000000000002',
  productId: '0198aabb-4444-7000-8000-000000000001',
  name: 'Grande',
  sku: null,
  sizeCode: 'G',
  prepMinutes: 10,
  isDefault: false,
  isActive: true,
  currentPrice: '52.00',
  currentPriceChannel: 'DineIn',
};

const noOpCreate = async () => pequena;
const noOpUpdate = async () => pequena;
const noOpSetPrice = async () => ({
  id: 'price-id',
  variantId: pequena.id,
  channel: 'DineIn' as const,
  amount: '35.00',
  validFrom: new Date().toISOString(),
  validTo: null,
});
const noOpActivate = async () => pequena;
const noOpDeactivate = async () => pequena;
const noOpMarkDefault = async () => pequena;

function renderEditor(overrides: Partial<React.ComponentProps<typeof VariantPriceEditor>> = {}) {
  return render(
    <VariantPriceEditor
      variants={[pequena, grande]}
      loading={false}
      onCreate={noOpCreate}
      onUpdate={noOpUpdate}
      onSetPrice={noOpSetPrice}
      onActivate={noOpActivate}
      onDeactivate={noOpDeactivate}
      onMarkDefault={noOpMarkDefault}
      {...overrides}
    />,
  );
}

describe('VariantPriceEditor', () => {
  it('lista as variações do produto em linha, com preço e situação', () => {
    renderEditor();

    expect(screen.getByLabelText('Nome da variação Pequena')).toHaveValue('Pequena');
    expect(screen.getByLabelText('Preço da variação Pequena')).toHaveValue('35,00');
    expect(screen.getByLabelText('Nome da variação Grande')).toHaveValue('Grande');
    expect(screen.getByLabelText('Preço da variação Grande')).toHaveValue('52,00');
    expect(screen.getByText('Padrão', { selector: 'span' })).toBeInTheDocument();
    expect(screen.getAllByText('Ativa')).toHaveLength(2);
  });

  it('avisa quando duas variações do mesmo produto têm o mesmo tamanho', () => {
    const media: VariantDto = { ...grande, id: 'media-id', name: 'Média', sizeCode: 'G' };
    renderEditor({ variants: [pequena, grande, media] });

    expect(screen.getAllByText('Tamanho repetido')).toHaveLength(2);
  });

  it('cria uma nova variação com o preço digitado na máscara de moeda', async () => {
    const onCreate = vi.fn(noOpCreate);
    renderEditor({ onCreate });

    fireEvent.click(screen.getByRole('button', { name: 'Nova variação' }));
    fireEvent.change(screen.getByLabelText('Nome da variação'), { target: { value: 'Família' } });
    fireEvent.change(screen.getByLabelText('Tamanho'), { target: { value: 'F' } });
    fireEvent.change(screen.getByLabelText('Preço'), { target: { value: '6000' } });
    fireEvent.click(screen.getByRole('button', { name: 'Adicionar variação' }));

    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith(
        expect.objectContaining({ name: 'Família', sizeCode: 'F', basePrice: '60.00' }),
      ),
    );
  });

  it('altera o preço vigente de uma variação existente', async () => {
    const onSetPrice = vi.fn(noOpSetPrice);
    const onUpdate = vi.fn(noOpUpdate);
    renderEditor({ onSetPrice, onUpdate });

    fireEvent.change(screen.getByLabelText('Preço da variação Pequena'), {
      target: { value: '3800' },
    });
    fireEvent.click(screen.getAllByRole('button', { name: 'Salvar' })[0]!);

    await waitFor(() => expect(onSetPrice).toHaveBeenCalledWith(pequena.id, { amount: '38.00' }));
    expect(onUpdate).toHaveBeenCalledWith(pequena.id, {
      name: 'Pequena',
      sizeCode: 'P',
      sku: undefined,
    });
  });

  it('desativa uma variação sem excluí-la fisicamente', async () => {
    const onDeactivate = vi.fn(noOpDeactivate);
    renderEditor({ onDeactivate });

    fireEvent.click(screen.getAllByRole('button', { name: 'Desativar' })[0]!);

    await waitFor(() => expect(onDeactivate).toHaveBeenCalledWith(pequena.id));
  });

  it('marca uma variação diferente como padrão', async () => {
    const onMarkDefault = vi.fn(noOpMarkDefault);
    renderEditor({ onMarkDefault });

    fireEvent.click(screen.getByRole('button', { name: 'Tornar padrão' }));

    await waitFor(() => expect(onMarkDefault).toHaveBeenCalledWith(grande.id));
  });
});
