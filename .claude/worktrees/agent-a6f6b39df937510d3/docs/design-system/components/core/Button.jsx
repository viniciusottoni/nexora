import React from 'react';

const SIZES = {
  sm: { height: 40, padding: '0 14px', fontSize: 13, clipPath: 'polygon(4px 0,100% 0,100% calc(100% - 4px),calc(100% - 4px) 100%,0 100%,0 4px)', gap: 6 },
  md: { height: 48, padding: '0 20px', fontSize: 15, clipPath: 'polygon(8px 0,100% 0,100% calc(100% - 8px),calc(100% - 8px) 100%,0 100%,0 8px)', gap: 8 },
  lg: { height: 54, padding: '0 36px', fontSize: 14, clipPath: 'polygon(10px 0,100% 0,100% calc(100% - 10px),calc(100% - 10px) 100%,0 100%,0 10px)', gap: 10 },
};

const VARIANTS = {
  primary: {
    background: 'var(--grad-energy)',
    color: 'var(--text-on-primary)',
    border: '1px solid transparent',
  },
  secondary: {
    background: 'var(--bg-elevated)',
    color: 'var(--text-primary)',
    border: '1px solid var(--border-default)',
  },
  ghost: {
    background: 'transparent',
    color: 'var(--text-secondary)',
    border: '1px solid transparent',
  },
  gold: {
    background: 'var(--grad-xp)',
    color: 'var(--text-on-gold)',
    border: '1px solid transparent',
  },
  danger: {
    background: 'var(--danger)',
    color: '#fff',
    border: '1px solid transparent',
  },
};

const GLOWS = {
  primary: 'var(--glow-blue)',
  gold: 'var(--glow-gold)',
  danger: 'var(--glow-danger)',
  secondary: 'none',
  ghost: 'none',
};

/**
 * Awaken Button — the primary action control.
 * Primary uses the signature blue→purple energy gradient; gold is the premium / XP CTA.
 */
export function Button({
  children,
  variant = 'primary',
  size = 'md',
  glow = false,
  fullWidth = false,
  disabled = false,
  leftIcon = null,
  rightIcon = null,
  type = 'button',
  onClick,
  style,
  ...rest
}) {
  const s = SIZES[size] || SIZES.md;
  const v = VARIANTS[variant] || VARIANTS.primary;

  return (
    <button
      type={type}
      disabled={disabled}
      onClick={onClick}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: s.gap,
        height: s.height,
        padding: s.padding,
        width: fullWidth ? '100%' : undefined,
        fontFamily: 'var(--font-display)',
        fontSize: s.fontSize,
        fontWeight: 700,
        letterSpacing: '0.08em',
        textTransform: 'uppercase',
        lineHeight: 1,
        clipPath: s.clipPath,
        border: 'none',
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.4 : 1,
        boxShadow: glow && !disabled ? GLOWS[variant] : 'none',
        transition: 'transform var(--dur-fast) var(--ease-out), box-shadow var(--dur-base) var(--ease-out), filter var(--dur-fast) var(--ease-out)',
        WebkitTapHighlightColor: 'transparent',
        ...v,
        ...style,
      }}
      onMouseDown={(e) => { if (!disabled) e.currentTarget.style.transform = 'scale(0.96)'; }}
      onMouseUp={(e) => { e.currentTarget.style.transform = 'scale(1)'; }}
      onMouseLeave={(e) => { e.currentTarget.style.transform = 'scale(1)'; e.currentTarget.style.filter = 'none'; }}
      onMouseEnter={(e) => { if (!disabled) e.currentTarget.style.filter = 'brightness(1.08)'; }}
      {...rest}
    >
      {leftIcon}
      {children}
      {rightIcon}
    </button>
  );
}
