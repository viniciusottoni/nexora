import React from 'react';

/**
 * Awaken Switch — toggle for settings (notifications, premium features, units).
 */
export function Switch({
  checked = false,
  onChange,
  disabled = false,
  label,
  style,
  ...rest
}) {
  const toggle = () => { if (!disabled && onChange) onChange(!checked); };
  const control = (
    <span
      role="switch"
      aria-checked={checked}
      onClick={toggle}
      style={{
        position: 'relative',
        width: 46,
        height: 28,
        flexShrink: 0,
        borderRadius: 'var(--radius-pill)',
        cursor: disabled ? 'not-allowed' : 'pointer',
        background: checked ? 'var(--grad-energy)' : 'var(--ink-700)',
        border: '1px solid',
        borderColor: checked ? 'transparent' : 'var(--border-default)',
        boxShadow: checked ? 'var(--glow-blue-sm)' : 'none',
        transition: 'background var(--dur-base) var(--ease-out), box-shadow var(--dur-base) var(--ease-out)',
        opacity: disabled ? 0.5 : 1,
      }}
    >
      <span
        style={{
          position: 'absolute',
          top: 3,
          left: checked ? 21 : 3,
          width: 20,
          height: 20,
          borderRadius: '50%',
          background: '#fff',
          boxShadow: '0 2px 4px rgba(0,0,0,0.4)',
          transition: 'left var(--dur-base) var(--ease-spring)',
        }}
      />
    </span>
  );

  if (!label) return React.cloneElement(control, { style: { ...control.props.style, ...style }, ...rest });
  return (
    <label style={{ display: 'inline-flex', alignItems: 'center', gap: 12, cursor: disabled ? 'not-allowed' : 'pointer', ...style }} {...rest}>
      {control}
      <span style={{ fontSize: 15, color: 'var(--text-primary)' }}>{label}</span>
    </label>
  );
}
