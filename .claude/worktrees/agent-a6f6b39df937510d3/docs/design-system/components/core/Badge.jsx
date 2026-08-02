import React from 'react';

const TONES = {
  blue:    { c: 'var(--blue-300)',   b: 'var(--blue-500)' },
  purple:  { c: 'var(--purple-300)', b: 'var(--purple-500)' },
  gold:    { c: 'var(--gold-400)',   b: 'var(--gold-500)' },
  green:   { c: '#7DEBA8',           b: 'var(--green-500)' },
  red:     { c: '#FF9C9C',           b: 'var(--red-500)' },
  cyan:    { c: 'var(--cyan-400)',   b: 'var(--cyan-400)' },
  neutral: { c: 'var(--mist-300)',   b: 'var(--mist-500)' },
};

/**
 * Awaken Badge — compact status / meta pill (PREMIUM, NOVO, streak counts, rank tags).
 */
export function Badge({
  children,
  tone = 'blue',
  variant = 'soft',
  icon = null,
  style,
  ...rest
}) {
  const t = TONES[tone] || TONES.blue;
  const looks = {
    soft: {
      background: `color-mix(in srgb, ${t.b} 16%, transparent)`,
      color: t.c,
      border: `1px solid color-mix(in srgb, ${t.b} 30%, transparent)`,
    },
    solid: {
      background: t.b,
      color: '#0A0B12',
      border: '1px solid transparent',
    },
    outline: {
      background: 'transparent',
      color: t.c,
      border: `1px solid color-mix(in srgb, ${t.b} 50%, transparent)`,
    },
  }[variant];

  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 5,
        height: 22,
        padding: '0 9px',
        fontFamily: 'var(--font-display)',
        fontSize: 11,
        fontWeight: 600,
        letterSpacing: '0.08em',
        textTransform: 'uppercase',
        lineHeight: 1,
        borderRadius: 'var(--radius-pill)',
        whiteSpace: 'nowrap',
        ...looks,
        ...style,
      }}
      {...rest}
    >
      {icon}
      {children}
    </span>
  );
}
