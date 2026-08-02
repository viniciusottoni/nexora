import * as React from 'react';

/**
 * Awaken XPBar — level + experience progress with the gold→amber XP fill.
 */
export interface XPBarProps extends React.HTMLAttributes<HTMLDivElement> {
  /** Current XP within the level. */
  value?: number;
  /** XP needed for next level. @default 100 */
  max?: number;
  /** Shows the "Nível N" label when provided. */
  level?: number;
  /** Show "value / max XP" readout. @default true */
  showValues?: boolean;
  /** Track height in px. @default 12 */
  height?: number;
}

export function XPBar(props: XPBarProps): JSX.Element;
