/** Botão só-ícone para barras, cabeçalhos e ações de linha. */
export interface IconButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  icon: string;
  size?: 'sm' | 'md' | 'lg';
  variant?: 'ghost' | 'solid' | 'outline';
  /** Contador sobreposto (alertas, chamados de mesa). */
  badge?: number | string;
  /** Obrigatório — vira aria-label e title. */
  label: string;
}
export function IconButton(props: IconButtonProps): JSX.Element;
