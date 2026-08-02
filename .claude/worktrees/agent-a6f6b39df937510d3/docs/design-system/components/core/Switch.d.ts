import * as React from 'react';

/**
 * Awaken Switch — settings toggle. On-state fills with the energy gradient.
 */
export interface SwitchProps {
  checked?: boolean;
  onChange?: (next: boolean) => void;
  disabled?: boolean;
  label?: React.ReactNode;
  style?: React.CSSProperties;
}

export function Switch(props: SwitchProps): JSX.Element;
