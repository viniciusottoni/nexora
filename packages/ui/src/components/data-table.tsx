import type { HTMLAttributes, Key, ReactNode } from 'react';

/** Tabela densa para caixa, estoque, financeiro e auditoria. */
export interface DataTableColumn<T> {
  readonly key: string;
  readonly header: ReactNode;
  /** Alinha à direita em mono tabular. */
  readonly numeric?: boolean;
  readonly align?: 'left' | 'center' | 'right';
  readonly width?: string;
  readonly render?: (row: T) => ReactNode;
}

export interface DataTableProps<T> extends HTMLAttributes<HTMLDivElement> {
  readonly columns: ReadonlyArray<DataTableColumn<T>>;
  readonly rows: ReadonlyArray<T>;
  /** Conteúdo de `<tfoot>` — linha de totais. */
  readonly footer?: ReactNode;
  readonly compact?: boolean;
  readonly onRowClick?: (row: T) => void;
  readonly rowKey?: string;
}

/** Alinhamento da célula: `numeric` já vem alinhado à direita pela classe `db-table__numeric`. */
function cellAlign<T>(column: DataTableColumn<T>): 'right' | 'center' | undefined {
  if (column.numeric) return undefined;
  if (column.align === 'right') return 'right';
  if (column.align === 'center') return 'center';
  return undefined;
}

export function DataTable<T = Record<string, unknown>>({
  columns,
  rows,
  footer,
  compact = false,
  onRowClick,
  rowKey,
  className = '',
  ...rest
}: Readonly<DataTableProps<T>>) {
  const cls = [
    'db-table',
    compact ? 'db-table--compact' : '',
    onRowClick ? 'db-table--clickable' : '',
  ]
    .filter(Boolean)
    .join(' ');
  return (
    <div {...rest} className={`db-table-wrap ${className}`.trim()}>
      <table className={cls}>
        <thead>
          <tr>
            {columns.map((column) => (
              <th
                key={column.key}
                style={{
                  textAlign:
                    column.align === 'right'
                      ? 'right'
                      : column.align === 'center'
                        ? 'center'
                        : 'left',
                  width: column.width,
                }}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => {
            const record = row as unknown as Record<string, unknown>;
            const key: Key = rowKey ? (record[rowKey] as Key) : index;
            return (
              <tr key={key} onClick={onRowClick ? () => onRowClick(row) : undefined}>
                {columns.map((column) => (
                  <td
                    key={column.key}
                    className={column.numeric ? 'db-table__numeric' : undefined}
                    /* O `align` da coluna precisa valer na célula também, não só no cabeçalho:
                       com só o `th` alinhado, o rótulo ia para a direita e o valor ficava na
                       esquerda — a coluna deixava de ler como coluna. */
                    style={{ textAlign: cellAlign(column) }}
                  >
                    {column.render ? column.render(row) : (record[column.key] as ReactNode)}
                  </td>
                ))}
              </tr>
            );
          })}
        </tbody>
        {footer ? <tfoot>{footer}</tfoot> : null}
      </table>
    </div>
  );
}
