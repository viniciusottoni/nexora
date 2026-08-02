// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { PublicMenuResponse, PublicTableInfoDto } from '@nexora/contracts';
import { TableMenuView } from './table-menu-view.js';

const table: PublicTableInfoDto = {
  id: '0198aabb-2222-7000-8000-000000000002',
  label: '12',
  areaName: 'Salão',
};

const menuWithProducts: PublicMenuResponse = {
  tenantId: '0198aabb-1111-7000-8000-000000000001',
  tenantName: 'Dona Betinha',
  categories: [
    {
      id: '0198aabb-3333-7000-8000-000000000003',
      name: 'Pizzas',
      description: null,
      position: 0,
      products: [
        {
          id: '0198aabb-4444-7000-8000-000000000004',
          name: 'Calabresa',
          description: 'Molho, mussarela, calabresa e cebola',
          ingredientsText: null,
          allergens: [],
          imageUrl: null,
          position: 0,
          fromPrice: '48.90',
        },
      ],
    },
  ],
};

describe('TableMenuView', () => {
  it('mostra a mesa/ambiente e o nome do estabelecimento (marca do tenant, US-021 §10)', () => {
    render(<TableMenuView table={table} menu={menuWithProducts} />);

    expect(screen.getByRole('heading', { name: 'Dona Betinha' })).toBeInTheDocument();
    expect(screen.getByText('Mesa 12 · Salão')).toBeInTheDocument();
  });

  it('lista os produtos com preco "a partir de"', () => {
    render(<TableMenuView table={table} menu={menuWithProducts} />);

    expect(screen.getByText('Calabresa')).toBeInTheDocument();
    expect(screen.getByText('A partir de R$ 48,90')).toBeInTheDocument();
  });

  it('mostra estado vazio quando o cardapio ainda nao tem categorias', () => {
    render(<TableMenuView table={table} menu={{ ...menuWithProducts, categories: [] }} />);

    expect(screen.getByText(/cardápio ainda está sendo preparado/i)).toBeInTheDocument();
  });
});
