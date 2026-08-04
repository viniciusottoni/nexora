/** Estado vazio. Diga o que fazer, não só que está vazio. */
export interface EmptyStateProps {
  icon?: string;
  title?: React.ReactNode;
  children?: React.ReactNode;
  action?: React.ReactNode;
}
export function EmptyState(props: EmptyStateProps): JSX.Element;
