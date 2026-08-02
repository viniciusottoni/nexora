import * as React from 'react';

/**
 * Awaken SystemWindow — the "System" HUD panel that announces a quest. A faceted,
 * glowing window themed by quest kind. Three kinds form the product taxonomy:
 *  - `daily`   (blue)   — Quest Diária: a grouped set of workout goals. Shows a penalty warning.
 *  - `dungeon` (purple) — a one-off side quest (single pontual workout).
 *  - `raid`    (red)    — group-only quest; activates when a squad is assembled.
 *
 * Each window lists its goals and the reward: XP plus 1-2 attribute points.
 *
 * @startingPoint section="Game" subtitle="System HUD window: daily / dungeon / raid" viewport="900x620"
 */
export interface SystemWindowGoal {
  label: React.ReactNode;
  /** Current progress (e.g. reps done). */
  current?: number;
  /** Target to reach. Omit + set `done` for binary goals. */
  target?: number;
  /** Unit suffix shown after the count (e.g. "reps", "min"). */
  unit?: string;
  /** For binary goals with no numeric target. */
  done?: boolean;
}

export interface SystemWindowReward {
  attr: 'strength' | 'agility' | 'endurance' | 'vitality' | 'focus' | 'wisdom';
  /** Points granted to that attribute. */
  amount: number;
}

export interface SystemWindowProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
  /** Quest taxonomy → theme color. @default "daily" */
  kind?: 'daily' | 'dungeon' | 'raid';
  title: React.ReactNode;
  /** Optional flavor line under the title. */
  description?: React.ReactNode;
  /** Optional difficulty rank letter (E → SSS). */
  rank?: string;
  /** Goal rows with progress bars. */
  goals?: SystemWindowGoal[];
  /** XP reward (rendered gold). @default 0 */
  xp?: number;
  /** Attribute rewards — typically 1-2. */
  rewards?: SystemWindowReward[];
  /**
   * Penalty note. Only rendered for `kind="daily"`. Pass a string to override the
   * default copy, or `false` to suppress it. @default (streak-reset copy)
   */
  warning?: string | false;
  /** Raid only — squad assembly progress. */
  participants?: { current: number; max: number };
  /** Optional action button label. */
  cta?: React.ReactNode;
  onCta?: () => void;
}

export function SystemWindow(props: SystemWindowProps): JSX.Element;
