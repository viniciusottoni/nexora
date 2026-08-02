import React from 'react';

/**
 * Awaken XPBar — level + experience progress. Gold→amber fill with a moving sheen.
 */
export function XPBar({
  value = 0,
  max = 100,
  level,
  showValues = true,
  height = 12,
  style,
  ...rest
}) {
  const pct = Math.max(0, Math.min(100, (value / max) * 100));
  return (
    <div style={{ ...style }} {...rest}>
      {(level != null || showValues) && (
        <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', marginBottom: 7 }}>
          {level != null && (
            <span style={{ fontFamily: 'var(--font-display)', fontWeight: 600, fontSize: 13, letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--gold-400)' }}>
              Nível {level}
            </span>
          )}
          {showValues && (
            <span className="tnum" style={{ fontFamily: 'var(--font-mono)', fontSize: 12, color: 'var(--text-tertiary)' }}>
              {Math.round(value)} / {max} XP
            </span>
          )}
        </div>
      )}
      <div
        style={{
          position: 'relative',
          height,
          borderRadius: 'var(--radius-pill)',
          background: 'var(--ink-700)',
          overflow: 'hidden',
          boxShadow: 'inset 0 1px 2px rgba(0,0,0,0.5)',
        }}
      >
        <div
          style={{
            position: 'absolute',
            inset: 0,
            width: `${pct}%`,
            borderRadius: 'var(--radius-pill)',
            background: 'var(--grad-xp)',
            boxShadow: '0 0 14px color-mix(in srgb, var(--gold-500) 55%, transparent)',
            transition: 'width var(--dur-epic) var(--ease-out)',
          }}
        >
          <span style={{ position: 'absolute', inset: 0, background: 'linear-gradient(180deg, rgba(255,255,255,0.45), transparent 60%)', borderRadius: 'inherit' }} />
        </div>
      </div>
    </div>
  );
}
