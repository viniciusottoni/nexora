/** Lista de escolha nativa com a moldura da Nexora. */
export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  size?: 'md' | 'lg';
  /** Strings ou `{value,label}`. Ignorado se `children` for passado. */
  options?: Array<string | { value: string; label: string }>;
}
export function Select(props: SelectProps): JSX.Element;
