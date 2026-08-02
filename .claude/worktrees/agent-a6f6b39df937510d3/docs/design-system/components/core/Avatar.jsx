import React from 'react';

const RANK_COLORS = {
  E: 'var(--rank-e)', D: 'var(--rank-d)', C: 'var(--rank-c)', B: 'var(--rank-b)',
  A: 'var(--rank-a)', S: 'var(--rank-s)', SS: 'var(--rank-ss)', SSS: 'var(--rank-sss)',
};

/**
 * Awaken Avatar — hunter portrait with optional rank ring and online dot.
 * Falls back to initials on the brand gradient.
 */
export function Avatar({
  src,
  name = '',
  size = 48,
  rank,
  online = false,
  style,
  ...rest
}) {
  const ring = rank ? RANK_COLORS[rank] : null;
  const initials = name.split(' ').filter(Boolean).slice(0, 2).map((w) => w[0]).join('').toUpperCase();
  const dot = Math.max(8, Math.round(size * 0.22));

  return (
    <div style={{ position: 'relative', width: size, height: size, flexShrink: 0, ...style }} {...rest}>
      <div
        style={{
          width: '100%',
          height: '100%',
          borderRadius: '50%',
          overflow: 'hidden',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: src ? 'var(--bg-elevated)' : 'var(--grad-energy)',
          border: ring ? `2px solid ${ring}` : '2px solid var(--border-default)',
          boxShadow: ring ? `0 0 14px color-mix(in srgb, ${ring} 45%, transparent)` : 'none',
          color: '#fff',
          fontFamily: 'var(--font-display)',
          fontWeight: 700,
          fontSize: size * 0.36,
          letterSpacing: '0.02em',
        }}
      >
        {src ? (
          <img src={src} alt={name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        ) : (
          initials || '?'
        )}
      </div>
      {online && (
        <span
          style={{
            position: 'absolute',
            right: 0,
            bottom: 0,
            width: dot,
            height: dot,
            borderRadius: '50%',
            background: 'var(--success)',
            border: '2px solid var(--bg-base)',
            boxShadow: '0 0 8px rgba(34,197,94,0.6)',
          }}
        />
      )}
    </div>
  );
}
