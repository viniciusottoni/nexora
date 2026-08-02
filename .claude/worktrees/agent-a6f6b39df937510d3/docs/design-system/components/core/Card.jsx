import React from 'react';

const RANK_COLORS = {
  E: 'var(--rank-e)', D: 'var(--rank-d)', C: 'var(--rank-c)', B: 'var(--rank-b)',
  A: 'var(--rank-a)', S: 'var(--rank-s)', SS: 'var(--rank-ss)', SSS: 'var(--rank-sss)',
};

/**
 * Awaken Card — the standard surface container. `energy` adds a faint gradient wash;
 * `glow` lights the border by rank for hero/hunter cards.
 */
export function Card({
  children,
  variant = 'default',
  rank,
  interactive = false,
  padding = 16,
  style,
  onClick,
  ...rest
}) {
  const glowColor = rank ? RANK_COLORS[rank] : 'var(--blue-400)';
  const base = {
    position: 'relative',
    clipPath: 'polygon(8px 0,100% 0,100% calc(100% - 8px),calc(100% - 8px) 100%,0 100%,0 8px)',
    padding,
    background: variant === 'energy' ? 'var(--grad-energy-soft), var(--bg-surface)' : 'var(--bg-surface)',
    border: '1px solid var(--border-default)',
    boxShadow: 'var(--shadow-md), var(--inset-sheen)',
    transition: 'transform var(--dur-base) var(--ease-out), box-shadow var(--dur-base) var(--ease-out), border-color var(--dur-base) var(--ease-out)',
    cursor: interactive || onClick ? 'pointer' : 'default',
  };
  if (variant === 'glow') {
    base.border = `1px solid color-mix(in srgb, ${glowColor} 55%, transparent)`;
    base.boxShadow = `0 0 0 1px color-mix(in srgb, ${glowColor} 20%, transparent), 0 0 28px color-mix(in srgb, ${glowColor} 28%, transparent), var(--inset-sheen)`;
  }
  return (
    <div
      onClick={onClick}
      style={{ ...base, ...style }}
      onMouseEnter={(e) => { if (interactive || onClick) { e.currentTarget.style.transform = 'translateY(-2px)'; e.currentTarget.style.borderColor = 'var(--border-strong)'; } }}
      onMouseLeave={(e) => { if (interactive || onClick) { e.currentTarget.style.transform = 'translateY(0)'; e.currentTarget.style.borderColor = variant === 'glow' ? `color-mix(in srgb, ${glowColor} 55%, transparent)` : 'var(--border-default)'; } }}
      {...rest}
    >
      {children}
    </div>
  );
}
