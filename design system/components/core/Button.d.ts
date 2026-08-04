/**
 * Botão de ação. `primary` usa a cor do tenant; `accent` é o verde Nexora de confirmação.
 */
export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'accent' | 'secondary' | 'ghost' | 'danger';
  /** `touch` (64px) e `lg` (48px) são obrigatórios em mesa, garçom e KDS. */
  size?: 'sm' | 'md' | 'lg' | 'touch';
  /** Nome de Icon à esquerda. */
  iconLeft?: string;
  iconRight?: string;
  block?: boolean;
  as?: 'button' | 'a';
}
export function Button(props: ButtonProps): JSX.Element;
