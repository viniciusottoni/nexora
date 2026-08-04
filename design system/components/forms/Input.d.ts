/**
 * Campo de texto.
 */
export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  size?: 'md' | 'lg';
  /** Ícone à esquerda (nome de Icon). */
  icon?: string;
  /** Texto fixo à direita: "%", "min", "kg". */
  suffix?: string;
  prefix?: string;
  invalid?: boolean;
  /** Alinha à direita em fonte mono tabular — use em valores e quantidades. */
  numeric?: boolean;
}
export function Input(props: InputProps): JSX.Element;
