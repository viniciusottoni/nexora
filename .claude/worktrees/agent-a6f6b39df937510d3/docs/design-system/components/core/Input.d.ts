import * as React from 'react';

/**
 * Awaken Input — dark text field with label, optional leading icon, hint and error.
 */
export interface InputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'style'> {
  label?: React.ReactNode;
  leftIcon?: React.ReactNode;
  rightSlot?: React.ReactNode;
  hint?: React.ReactNode;
  error?: React.ReactNode;
  style?: React.CSSProperties;
}

export function Input(props: InputProps): JSX.Element;
