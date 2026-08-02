import React from 'react';

const ATTRS = {
  strength:  { label: 'Força',       color: 'var(--attr-strength)' },
  agility:   { label: 'Agilidade',   color: 'var(--attr-agility)' },
  endurance: { label: 'Resistência', color: 'var(--attr-endurance)' },
  vitality:  { label: 'Vitalidade',  color: 'var(--attr-vitality)' },
  focus:     { label: 'Foco',        color: 'var(--attr-focus)' },
  wisdom:    { label: 'Sabedoria',   color: 'var(--attr-wisdom)' },
};

/**
 * Awaken StatBar — a single character attribute (Força, Agilidade, …) as an RPG
 * status bar. Color is fixed per attribute.
 */
export function StatBar({
  attr = 'strength',
  value = 0,
  max = 100,
  label,
  style,
  ...rest
}) {
  const a = ATTRS[attr] || ATTRS.strength;
  const pct = Math.max(0, Math.min(100, (value / max) * 100));
  return (
    <div style={{ ...style }} {...rest}>
      <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', marginBottom: 6 }}>
        <span style={{ fontFamily: 'var(--font-display)', fontSize: 12, fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--text-secondary)' }}>
          {label || a.label}
        </span>
        <span className="tnum" style={{ fontFamily: 'var(--font-mono)', fontSize: 13, fontWeight: 700, color: a.color }}>
          {Math.round(value)}
        </span>
      </div>
      <div style={{ height: 8, borderRadius: 'var(--radius-pill)', background: 'var(--ink-700)', overflow: 'hidden', boxShadow: 'inset 0 1px 2px rgba(0,0,0,0.5)' }}>
        <div
          style={{
            height: '100%',
            width: `${pct}%`,
            borderRadius: 'var(--radius-pill)',
            background: `linear-gradient(90deg, color-mix(in srgb, ${a.color} 70%, #000), ${a.color})`,
            boxShadow: `0 0 10px color-mix(in srgb, ${a.color} 55%, transparent)`,
            transition: 'width var(--dur-slow) var(--ease-out)',
          }}
        />
      </div>
    </div>
  );
}
