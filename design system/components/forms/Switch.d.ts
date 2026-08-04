/** Liga/desliga de configuração — efeito imediato, sem botão de salvar. */
export interface SwitchProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: React.ReactNode;
  description?: React.ReactNode;
}
export function Switch(props: SwitchProps): JSX.Element;
