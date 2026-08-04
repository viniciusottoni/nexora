/** Contêiner padrão: fundo branco, borda 1px sutil, raio do tenant, sombra rasa. */
export interface CardProps {
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
  /** Ações no canto superior direito (IconButton, Button ghost, SegmentedControl). */
  actions?: React.ReactNode;
  footer?: React.ReactNode;
  children?: React.ReactNode;
  elevation?: 'flat' | 'card' | 'raised';
  interactive?: boolean;
  padding?: 'default' | 'tight' | 'none';
}
export function Card(props: CardProps): JSX.Element;
