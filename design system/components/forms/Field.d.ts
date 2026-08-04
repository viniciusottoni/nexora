/** Envelope de rótulo, dica e erro para qualquer controle de formulário. */
export interface FieldProps {
  label?: React.ReactNode;
  hint?: React.ReactNode;
  /** Quando presente substitui a dica e pinta a mensagem em vermelho. */
  error?: React.ReactNode;
  required?: boolean;
  htmlFor?: string;
  children?: React.ReactNode;
}
export function Field(props: FieldProps): JSX.Element;
