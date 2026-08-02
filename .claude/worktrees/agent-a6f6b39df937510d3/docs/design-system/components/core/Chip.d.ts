import * as React from 'react';

/**
 * Awaken Chip — selectable token for onboarding answers, filters and tags. Lights up
 * with brand energy when selected. 44px min hit target.
 */
export interface ChipProps {
  children?: React.ReactNode;
  selected?: boolean;
  icon?: React.ReactNode;
  disabled?: boolean;
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
  style?: React.CSSProperties;
}

export function Chip(props: ChipProps): JSX.Element;
