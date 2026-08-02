import React from 'react';

const RANKS = {
  E:   { color: 'var(--rank-e)',   grad: 'linear-gradient(160deg, #8A92A3, #4B5160)' },
  D:   { color: 'var(--rank-d)',   grad: 'linear-gradient(160deg, #4CE07F, #15803D)' },
  C:   { color: 'var(--rank-c)',   grad: 'linear-gradient(160deg, #5B9BFF, #1D4ED8)' },
  B:   { color: 'var(--rank-b)',   grad: 'linear-gradient(160deg, #C07BFF, #7E22CE)' },
  A:   { color: 'var(--rank-a)',   grad: 'linear-gradient(160deg, #FACC15, #B8860B)' },
  S:   { color: 'var(--rank-s)',   grad: 'linear-gradient(160deg, #FF6B5B, #C81E2C)' },
  SS:  { color: 'var(--rank-ss)',  grad: 'var(--grad-rank-ss)' },
  SSS: { color: 'var(--rank-sss)', grad: 'var(--grad-rank-sss)' },
};

const DOT_SIZE = 0.09; // corner dot relative to size

/**
 * Awaken RankBadge — the hexagonal rank emblem (E → SSS). The core gamification motif:
 * a faceted shield that glows in the rank's color.
 */
export function RankBadge({
  rank = 'E',
  size = 64,
  glow = true,
  style,
  ...rest
}) {
  const r = RANKS[rank] || RANKS.E;
  const d = size * DOT_SIZE;
  const inset = size * 0.06;

  return (
    <div
      style={{
        position: 'relative',
        width: size,
        height: size,
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        filter: glow ? `drop-shadow(0 0 ${size * 0.22}px color-mix(in srgb, ${r.color} 65%, transparent))` : 'none',
        ...style,
      }}
      {...rest}
    >
      {/* outer diamond — gradient border */}
      <div style={{ position: 'absolute', inset: 0, background: r.grad, transform: 'rotate(45deg)' }} />
      {/* inner diamond — dark well */}
      <div style={{ position: 'absolute', inset, background: 'var(--bg-base)', transform: 'rotate(45deg)' }} />
      {/* sheen overlay */}
      <div style={{ position: 'absolute', inset, background: `linear-gradient(180deg, color-mix(in srgb, ${r.color} 18%, transparent), transparent 55%)`, transform: 'rotate(45deg)' }} />
      {/* corner dots at bounding-box corners — matches onboarding diamond mark */}
      {[{ top: -d/2, left: -d/2 }, { top: -d/2, left: size - d/2 }, { top: size - d/2, left: -d/2 }, { top: size - d/2, left: size - d/2 }].map((pos, i) => (
        <span key={i} style={{ position: 'absolute', ...pos, width: d, height: d, background: r.color, transform: 'rotate(45deg)', boxShadow: `0 0 ${d * 2}px ${r.color}` }} />
      ))}
      {/* rank label — upright */}
      <span
        style={{
          position: 'relative',
          fontFamily: 'var(--font-display)',
          fontWeight: 700,
          fontSize: size * (rank.length === 3 ? 0.28 : rank.length === 2 ? 0.36 : 0.46),
          letterSpacing: '-0.01em',
          lineHeight: 1,
          color: r.color,
          textShadow: `0 0 ${size * 0.14}px color-mix(in srgb, ${r.color} 80%, transparent)`,
        }}
      >
        {rank}
      </span>
    </div>
  );
}
