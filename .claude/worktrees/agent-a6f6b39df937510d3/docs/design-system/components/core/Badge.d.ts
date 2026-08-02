import * as React from 'react';

/**
 * Awaken Badge — compact status / meta pill. Uppercase display micro-type for tags
 * like PREMIUM, NOVO, streak counts and rank labels.
 */
export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  children?: React.ReactNode;
  /** @default "blue" */
  tone?: 'blue' | 'purple' | 'gold' | 'green' | 'red' | 'cyan' | 'neutral';
  /** @default "soft" */
  variant?: 'soft' | 'solid' | 'outline';
  icon?: React.ReactNode;
}

export function Badge(props: BadgeProps): JSX.Element;
