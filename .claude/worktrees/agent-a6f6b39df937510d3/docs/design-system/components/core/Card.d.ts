import * as React from 'react';

/**
 * Awaken Card — standard surface container. `energy` washes a faint brand gradient;
 * `glow` lights the border by `rank` for hunter / achievement cards.
 *
 * @startingPoint section="Core" subtitle="Surface cards: default, energy, rank-glow" viewport="700x240"
 */
export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  children?: React.ReactNode;
  /** @default "default" */
  variant?: 'default' | 'energy' | 'glow';
  /** Rank used to tint the glow border when variant="glow". */
  rank?: 'E' | 'D' | 'C' | 'B' | 'A' | 'S' | 'SS' | 'SSS';
  /** Lift + brighten border on hover. @default false */
  interactive?: boolean;
  /** Inner padding in px. @default 16 */
  padding?: number;
}

export function Card(props: CardProps): JSX.Element;
