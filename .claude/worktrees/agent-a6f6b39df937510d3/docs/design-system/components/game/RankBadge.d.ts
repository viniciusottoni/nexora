import * as React from 'react';

/**
 * Awaken RankBadge — the hexagonal rank emblem (E → SSS), the core gamification motif.
 * A faceted shield glowing in the rank's color.
 *
 * @startingPoint section="Game" subtitle="Hexagonal rank emblems E → SSS" viewport="700x180"
 */
export interface RankBadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  /** @default "E" */
  rank?: 'E' | 'D' | 'C' | 'B' | 'A' | 'S' | 'SS' | 'SSS';
  /** Size in px (badge is square). @default 64 */
  size?: number;
  /** Colored drop-shadow halo. @default true */
  glow?: boolean;
}

export function RankBadge(props: RankBadgeProps): JSX.Element;
