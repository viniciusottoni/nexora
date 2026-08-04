/** Alternador de escopo: período, canal, praça de produção, ambiente do salão. */
export interface SegmentedControlProps {
  options: Array<string | { value: string; label: React.ReactNode; icon?: string }>;
  value?: string;
  onChange?: (value: string) => void;
  /** `lg` para operação de toque. */
  size?: 'md' | 'lg';
  block?: boolean;
}
export function SegmentedControl(props: SegmentedControlProps): JSX.Element;
