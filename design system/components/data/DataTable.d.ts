/** Tabela densa para caixa, estoque, financeiro e auditoria. */
export interface DataTableColumn<T = any> {
  key: string;
  header: React.ReactNode;
  /** Alinha à direita em mono tabular. */
  numeric?: boolean;
  align?: 'left' | 'center' | 'right';
  width?: string;
  render?: (row: T) => React.ReactNode;
}
export interface DataTableProps<T = any> {
  columns: DataTableColumn<T>[];
  rows: T[];
  /** Conteúdo de `<tfoot>` — linha de totais. */
  footer?: React.ReactNode;
  compact?: boolean;
  onRowClick?: (row: T) => void;
  rowKey?: string;
}
export function DataTable<T = any>(props: DataTableProps<T>): JSX.Element;
