import * as React from 'react';

/**
 * Awaken QuestCard — a daily quest row, the product's core loop unit. Shows the
 * exercise/quest, the attribute it trains, the XP reward and a completion toggle.
 *
 * @startingPoint section="Game" subtitle="Daily quest rows: todo / active / done" viewport="700x300"
 */
export interface QuestCardProps extends React.HTMLAttributes<HTMLDivElement> {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  /** XP reward shown on the right. @default 0 */
  xp?: number;
  /** Single attribute trained — drives one colored tag (no amount). Prefer `rewards`. */
  attr?: 'strength' | 'agility' | 'endurance' | 'vitality' | 'focus' | 'wisdom';
  /** Attribute rewards with point amounts — typically 1-2. Overrides `attr` when set. */
  rewards?: Array<{ attr: 'strength' | 'agility' | 'endurance' | 'vitality' | 'focus' | 'wisdom'; amount?: number }>;
  /** @default "todo" */
  status?: 'todo' | 'active' | 'done';
  /** Leading icon (exercise glyph). */
  icon?: React.ReactNode;
  /** Fired when the completion circle is tapped. */
  onToggle?: () => void;
}

export function QuestCard(props: QuestCardProps): JSX.Element;
