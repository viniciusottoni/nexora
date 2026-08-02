import * as React from 'react';

/**
 * Awaken StatBar — one character attribute as an RPG status bar. Each attribute has a
 * fixed color (Força=red, Agilidade=teal, Resistência=blue, Vitalidade=gold, Foco=purple, Sabedoria=cyan).
 */
export interface StatBarProps extends React.HTMLAttributes<HTMLDivElement> {
  /** @default "strength" */
  attr?: 'strength' | 'agility' | 'endurance' | 'vitality' | 'focus' | 'wisdom';
  value?: number;
  /** @default 100 */
  max?: number;
  /** Override the default PT-BR attribute label. */
  label?: React.ReactNode;
}

export function StatBar(props: StatBarProps): JSX.Element;
