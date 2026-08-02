import * as React from 'react';

/**
 * Awaken ProgressRing — circular progress for daily goals (água, quests, calorias).
 * Pass color="energy" for the brand gradient stroke.
 */
export interface ProgressRingProps extends React.HTMLAttributes<HTMLDivElement> {
  value?: number;
  /** @default 100 */
  max?: number;
  /** Diameter in px. @default 96 */
  size?: number;
  /** Stroke width in px. @default 9 */
  stroke?: number;
  /** A CSS color, or "energy" for the blue→purple gradient. @default "var(--blue-400)" */
  color?: string;
  trackColor?: string;
  /** Big center value (defaults to the percentage). */
  label?: React.ReactNode;
  sublabel?: React.ReactNode;
  /** Custom center content (overrides label/sublabel). */
  children?: React.ReactNode;
}

export function ProgressRing(props: ProgressRingProps): JSX.Element;
