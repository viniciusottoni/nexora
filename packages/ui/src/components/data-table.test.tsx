// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { DataTable, type DataTableColumn } from './data-table.js';

interface Produto {
  id: string;
  produto: string;
  cmv: string;
}

const columns: DataTableColumn<Produto>[] = [
  { key: 'produto', header: 'Produto' },
  { key: 'cmv', header: 'CMV', numeric: true },
];

const rows: Produto[] = [
  { id: '1', produto: 'Pizza Marguerita', cmv: 'R$ 12,40' },
  { id: '2', produto: 'Pizza Calabresa', cmv: 'R$ 14,10' },
];

describe('DataTable', () => {
  it('renderiza cabeçalho e linhas a partir de columns/rows', () => {
    render(<DataTable columns={columns} rows={rows} rowKey="id" />);

    expect(screen.getByRole('columnheader', { name: 'Produto' })).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Pizza Marguerita' })).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'R$ 14,10' })).toHaveClass('db-table__numeric');
  });

  it('marca a tabela como clicável e propaga a linha ao clique', () => {
    const onRowClick = vi.fn();
    render(<DataTable columns={columns} rows={rows} rowKey="id" onRowClick={onRowClick} />);

    screen.getByRole('cell', { name: 'Pizza Calabresa' }).closest('tr')?.click();

    expect(onRowClick).toHaveBeenCalledWith(rows[1]);
    expect(document.querySelector('table')).toHaveClass('db-table--clickable');
  });
});
