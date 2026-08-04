/** Contador de quantidade em pílula — carrinho da mesa, app do garçom, entrada de estoque. */
export interface QuantityStepperProps {
  value?: number;
  min?: number;
  max?: number;
  onChange?: (value: number) => void;
  size?: 'sm' | 'md';
}
export function QuantityStepper(props: QuantityStepperProps): JSX.Element;
