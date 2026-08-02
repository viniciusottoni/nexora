import * as React from 'react';

/**
 * Awaken Avatar — hunter portrait with optional rank ring and online dot. Falls back
 * to initials on the brand gradient when no image is supplied.
 */
export interface AvatarProps extends React.HTMLAttributes<HTMLDivElement> {
  src?: string;
  name?: string;
  /** Diameter in px. @default 48 */
  size?: number;
  /** Tints the ring + halo by rank. */
  rank?: 'E' | 'D' | 'C' | 'B' | 'A' | 'S' | 'SS' | 'SSS';
  online?: boolean;
}

export function Avatar(props: AvatarProps): JSX.Element;
