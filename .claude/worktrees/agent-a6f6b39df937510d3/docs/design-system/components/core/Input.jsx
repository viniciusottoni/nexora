import React from 'react';

/**
 * Awaken Input — dark text field with label, optional leading icon, hint and error.
 */
export function Input({
  label,
  value,
  onChange,
  placeholder,
  type = 'text',
  leftIcon = null,
  rightSlot = null,
  hint,
  error,
  disabled = false,
  style,
  ...rest
}) {
  const [focused, setFocused] = React.useState(false);
  const borderColor = error
    ? 'var(--danger)'
    : focused
    ? 'var(--border-focus)'
    : 'var(--border-default)';

  return (
    <label style={{ display: 'block', ...style }}>
      {label && (
        <span className="eyebrow" style={{ display: 'block', marginBottom: 8, color: 'var(--text-secondary)' }}>
          {label}
        </span>
      )}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 10,
          height: 'var(--control-md)',
          padding: '0 14px',
          clipPath: 'polygon(6px 0,100% 0,100% calc(100% - 6px),calc(100% - 6px) 100%,0 100%,0 6px)',
          background: 'var(--bg-input)',
          border: `1px solid ${borderColor}`,
          boxShadow: focused ? 'var(--ring-focus)' : 'none',
          transition: 'border-color var(--dur-fast) var(--ease-out), box-shadow var(--dur-fast) var(--ease-out)',
          opacity: disabled ? 0.5 : 1,
        }}
      >
        {leftIcon && <span style={{ display: 'flex', color: 'var(--text-tertiary)' }}>{leftIcon}</span>}
        <input
          type={type}
          value={value}
          onChange={onChange}
          placeholder={placeholder}
          disabled={disabled}
          onFocus={() => setFocused(true)}
          onBlur={() => setFocused(false)}
          style={{
            flex: 1,
            minWidth: 0,
            height: '100%',
            border: 'none',
            outline: 'none',
            background: 'transparent',
            color: 'var(--text-primary)',
            fontFamily: 'var(--font-body)',
            fontSize: 15,
          }}
          {...rest}
        />
        {rightSlot}
      </div>
      {(hint || error) && (
        <span style={{ display: 'block', marginTop: 6, fontSize: 12, color: error ? 'var(--danger)' : 'var(--text-tertiary)' }}>
          {error || hint}
        </span>
      )}
    </label>
  );
}
