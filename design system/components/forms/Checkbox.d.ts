/** Caixa de marcação (`type="radio"` para escolha única). Alvo de 48px por padrão. */
export interface CheckboxProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: React.ReactNode;
  type?: 'checkbox' | 'radio';
  /** Valor à direita — usado em grupos de modificadores do cardápio. */
  price?: string;
  /** Remove a altura mínima de toque (use só em listas densas de desktop). */
  compact?: boolean;
}
export function Checkbox(props: CheckboxProps): JSX.Element;
