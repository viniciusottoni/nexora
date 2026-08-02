import * as React from 'react';

/**
 * Awaken Button — primary action control. Primary = signature blue→purple energy
 * gradient; gold = premium / upgrade CTA; ghost/secondary for lower emphasis.
 *
 * @startingPoint section="Core" subtitle="Buttons: primary, secondary, ghost, gold, danger" viewport="700x200"
 */
export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  children?: React.ReactNode;
  /** Visual emphasis. @default "primary" */
  variant?: 'primary' | 'secondary' | 'ghost' | 'gold' | 'danger';
  /** @default "md" */
  size?: 'sm' | 'md' | 'lg';
  /** Add the energy glow (use sparingly, for hero CTAs). @default false */
  glow?: boolean;
  fullWidth?: boolean;
  disabled?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

export function Button(props: ButtonProps): JSX.Element;
