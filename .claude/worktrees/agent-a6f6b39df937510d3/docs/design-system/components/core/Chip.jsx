import React from 'react';

/**
 * Awaken Chip — selectable token for onboarding choices, filters and tags.
 * Selected state lights up with the brand energy.
 */
export function Chip({
  children,
  selected = false,
  icon = null,
  disabled = false,
  onClick,
  style,
  ...rest
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      aria-pressed={selected}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 8,
        minHeight: 44,
        padding: '10px 16px',
        fontFamily: 'var(--font-body)',
        fontSize: 14,
        fontWeight: 500,
        lineHeight: 1.1,
        borderRadius: 'var(--radius-pill)',
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.4 : 1,
        color: selected ? '#fff' : 'var(--text-secondary)',
        background: selected ? 'var(--grad-energy-soft), var(--bg-elevated)' : 'var(--bg-input)',
        border: selected ? '1px solid color-mix(in srgb, var(--blue-400) 60%, transparent)' : '1px solid var(--border-default)',
        boxShadow: selected ? 'var(--glow-blue-sm)' : 'none',
        transition: 'all var(--dur-fast) var(--ease-out)',
        WebkitTapHighlightColor: 'transparent',
        ...style,
      }}
      {...rest}
    >
      {icon}
      {children}
    </button>
  );
}
