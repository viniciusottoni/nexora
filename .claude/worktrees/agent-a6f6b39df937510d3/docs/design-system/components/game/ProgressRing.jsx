import React from 'react';

/**
 * Awaken ProgressRing — circular progress for daily goals (water, quests done, calories).
 * Defaults to the energy gradient via SVG stroke.
 */
export function ProgressRing({
  value = 0,
  max = 100,
  size = 96,
  stroke = 9,
  color = 'var(--blue-400)',
  trackColor = 'var(--ink-700)',
  label,
  sublabel,
  children,
  style,
  ...rest
}) {
  const pct = Math.max(0, Math.min(1, value / max));
  const r = (size - stroke) / 2;
  const circ = 2 * Math.PI * r;
  const gid = React.useId();

  return (
    <div style={{ position: 'relative', width: size, height: size, ...style }} {...rest}>
      <svg width={size} height={size} style={{ transform: 'rotate(-90deg)' }}>
        <defs>
          <linearGradient id={gid} x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#2D6FF5" />
            <stop offset="100%" stopColor="#8B3FD8" />
          </linearGradient>
        </defs>
        <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke={trackColor} strokeWidth={stroke} />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={r}
          fill="none"
          stroke={color === 'energy' ? `url(#${gid})` : color}
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={circ}
          strokeDashoffset={circ * (1 - pct)}
          style={{ transition: 'stroke-dashoffset var(--dur-epic) var(--ease-out)', filter: 'drop-shadow(0 0 6px rgba(45,111,245,0.5))' }}
        />
      </svg>
      <div style={{ position: 'absolute', inset: 0, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', textAlign: 'center' }}>
        {children || (
          <>
            <span className="tnum" style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: size * 0.26, color: 'var(--text-primary)', lineHeight: 1 }}>{label != null ? label : Math.round(pct * 100)}</span>
            {sublabel && <span style={{ fontFamily: 'var(--font-mono)', fontSize: 10, letterSpacing: '0.08em', color: 'var(--text-tertiary)', marginTop: 3, textTransform: 'uppercase' }}>{sublabel}</span>}
          </>
        )}
      </div>
    </div>
  );
}
