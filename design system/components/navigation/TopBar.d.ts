/** Barra superior de 56px. O slot `right` sempre carrega o `SyncStatus`. */
export interface TopBarProps {
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
  left?: React.ReactNode;
  right?: React.ReactNode;
  variant?: 'card' | 'sunken' | 'brand';
}
export function TopBar(props: TopBarProps): JSX.Element;
