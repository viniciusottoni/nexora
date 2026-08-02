/* @ds-bundle: {"format":3,"namespace":"AwakenDesignSystem_956798","components":[{"name":"Avatar","sourcePath":"components/core/Avatar.jsx"},{"name":"Badge","sourcePath":"components/core/Badge.jsx"},{"name":"Button","sourcePath":"components/core/Button.jsx"},{"name":"Card","sourcePath":"components/core/Card.jsx"},{"name":"Chip","sourcePath":"components/core/Chip.jsx"},{"name":"Input","sourcePath":"components/core/Input.jsx"},{"name":"Switch","sourcePath":"components/core/Switch.jsx"},{"name":"ProgressRing","sourcePath":"components/game/ProgressRing.jsx"},{"name":"QuestCard","sourcePath":"components/game/QuestCard.jsx"},{"name":"RankBadge","sourcePath":"components/game/RankBadge.jsx"},{"name":"StatBar","sourcePath":"components/game/StatBar.jsx"},{"name":"SystemWindow","sourcePath":"components/game/SystemWindow.jsx"},{"name":"XPBar","sourcePath":"components/game/XPBar.jsx"}],"sourceHashes":{"components/core/Avatar.jsx":"e4bd9f1c250f","components/core/Badge.jsx":"3cf8eac4a3bd","components/core/Button.jsx":"357ee7ae8d42","components/core/Card.jsx":"5efd0dc87dc0","components/core/Chip.jsx":"00fdb3847994","components/core/Input.jsx":"388b0a460ce0","components/core/Switch.jsx":"a6e708745fd3","components/game/ProgressRing.jsx":"b0764e798d95","components/game/QuestCard.jsx":"632b468fc8ef","components/game/RankBadge.jsx":"f2cf98d0b993","components/game/StatBar.jsx":"e0f3994407b9","components/game/SystemWindow.jsx":"cece3e69641c","components/game/XPBar.jsx":"60301f498f4a","player-screen/app.jsx":"fa93e89bd24b","player-screen/ui.jsx":"c53cff3967fb","ui_kits/app/app.jsx":"f5fc09d8f3f0","ui_kits/app/chrome.jsx":"d3f4b1d7b2b8","ui_kits/app/icons.jsx":"10a9975831b7","ui_kits/app/screens-main.jsx":"1bdaaf2bad2f","ui_kits/app/screens-onboarding.jsx":"5e71e814153d","ui_kits/app/screens-player.jsx":"dfe38f4638bf"},"inlinedExternals":[],"unexposedExports":[]} */

(() => {

const __ds_ns = (window.AwakenDesignSystem_956798 = window.AwakenDesignSystem_956798 || {});

const __ds_scope = {};

(__ds_ns.__errors = __ds_ns.__errors || []);

// components/core/Avatar.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const RANK_COLORS = {
  E: 'var(--rank-e)',
  D: 'var(--rank-d)',
  C: 'var(--rank-c)',
  B: 'var(--rank-b)',
  A: 'var(--rank-a)',
  S: 'var(--rank-s)',
  SS: 'var(--rank-ss)',
  SSS: 'var(--rank-sss)'
};

/**
 * Awaken Avatar — hunter portrait with optional rank ring and online dot.
 * Falls back to initials on the brand gradient.
 */
function Avatar({
  src,
  name = '',
  size = 48,
  rank,
  online = false,
  style,
  ...rest
}) {
  const ring = rank ? RANK_COLORS[rank] : null;
  const initials = name.split(' ').filter(Boolean).slice(0, 2).map(w => w[0]).join('').toUpperCase();
  const dot = Math.max(8, Math.round(size * 0.22));
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      position: 'relative',
      width: size,
      height: size,
      flexShrink: 0,
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("div", {
    style: {
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
      letterSpacing: '0.02em'
    }
  }, src ? /*#__PURE__*/React.createElement("img", {
    src: src,
    alt: name,
    style: {
      width: '100%',
      height: '100%',
      objectFit: 'cover'
    }
  }) : initials || '?'), online && /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      right: 0,
      bottom: 0,
      width: dot,
      height: dot,
      borderRadius: '50%',
      background: 'var(--success)',
      border: '2px solid var(--bg-base)',
      boxShadow: '0 0 8px rgba(34,197,94,0.6)'
    }
  }));
}
Object.assign(__ds_scope, { Avatar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Avatar.jsx", error: String((e && e.message) || e) }); }

// components/core/Badge.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const TONES = {
  blue: {
    c: 'var(--blue-300)',
    b: 'var(--blue-500)'
  },
  purple: {
    c: 'var(--purple-300)',
    b: 'var(--purple-500)'
  },
  gold: {
    c: 'var(--gold-400)',
    b: 'var(--gold-500)'
  },
  green: {
    c: '#7DEBA8',
    b: 'var(--green-500)'
  },
  red: {
    c: '#FF9C9C',
    b: 'var(--red-500)'
  },
  cyan: {
    c: 'var(--cyan-400)',
    b: 'var(--cyan-400)'
  },
  neutral: {
    c: 'var(--mist-300)',
    b: 'var(--mist-500)'
  }
};

/**
 * Awaken Badge — compact status / meta pill (PREMIUM, NOVO, streak counts, rank tags).
 */
function Badge({
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
      border: `1px solid color-mix(in srgb, ${t.b} 30%, transparent)`
    },
    solid: {
      background: t.b,
      color: '#0A0B12',
      border: '1px solid transparent'
    },
    outline: {
      background: 'transparent',
      color: t.c,
      border: `1px solid color-mix(in srgb, ${t.b} 50%, transparent)`
    }
  }[variant];
  return /*#__PURE__*/React.createElement("span", _extends({
    style: {
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
      ...style
    }
  }, rest), icon, children);
}
Object.assign(__ds_scope, { Badge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Badge.jsx", error: String((e && e.message) || e) }); }

// components/core/Button.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const SIZES = {
  sm: {
    height: 40,
    padding: '0 14px',
    fontSize: 13,
    clipPath: 'polygon(4px 0,100% 0,100% calc(100% - 4px),calc(100% - 4px) 100%,0 100%,0 4px)',
    gap: 6
  },
  md: {
    height: 48,
    padding: '0 20px',
    fontSize: 15,
    clipPath: 'polygon(8px 0,100% 0,100% calc(100% - 8px),calc(100% - 8px) 100%,0 100%,0 8px)',
    gap: 8
  },
  lg: {
    height: 54,
    padding: '0 36px',
    fontSize: 14,
    clipPath: 'polygon(10px 0,100% 0,100% calc(100% - 10px),calc(100% - 10px) 100%,0 100%,0 10px)',
    gap: 10
  }
};
const VARIANTS = {
  primary: {
    background: 'var(--grad-energy)',
    color: 'var(--text-on-primary)',
    border: '1px solid transparent'
  },
  secondary: {
    background: 'var(--bg-elevated)',
    color: 'var(--text-primary)',
    border: '1px solid var(--border-default)'
  },
  ghost: {
    background: 'transparent',
    color: 'var(--text-secondary)',
    border: '1px solid transparent'
  },
  gold: {
    background: 'var(--grad-xp)',
    color: 'var(--text-on-gold)',
    border: '1px solid transparent'
  },
  danger: {
    background: 'var(--danger)',
    color: '#fff',
    border: '1px solid transparent'
  }
};
const GLOWS = {
  primary: 'var(--glow-blue)',
  gold: 'var(--glow-gold)',
  danger: 'var(--glow-danger)',
  secondary: 'none',
  ghost: 'none'
};

/**
 * Awaken Button — the primary action control.
 * Primary uses the signature blue→purple energy gradient; gold is the premium / XP CTA.
 */
function Button({
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
  return /*#__PURE__*/React.createElement("button", _extends({
    type: type,
    disabled: disabled,
    onClick: onClick,
    style: {
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
      ...style
    },
    onMouseDown: e => {
      if (!disabled) e.currentTarget.style.transform = 'scale(0.96)';
    },
    onMouseUp: e => {
      e.currentTarget.style.transform = 'scale(1)';
    },
    onMouseLeave: e => {
      e.currentTarget.style.transform = 'scale(1)';
      e.currentTarget.style.filter = 'none';
    },
    onMouseEnter: e => {
      if (!disabled) e.currentTarget.style.filter = 'brightness(1.08)';
    }
  }, rest), leftIcon, children, rightIcon);
}
Object.assign(__ds_scope, { Button });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Button.jsx", error: String((e && e.message) || e) }); }

// components/core/Card.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const RANK_COLORS = {
  E: 'var(--rank-e)',
  D: 'var(--rank-d)',
  C: 'var(--rank-c)',
  B: 'var(--rank-b)',
  A: 'var(--rank-a)',
  S: 'var(--rank-s)',
  SS: 'var(--rank-ss)',
  SSS: 'var(--rank-sss)'
};

/**
 * Awaken Card — the standard surface container. `energy` adds a faint gradient wash;
 * `glow` lights the border by rank for hero/hunter cards.
 */
function Card({
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
    cursor: interactive || onClick ? 'pointer' : 'default'
  };
  if (variant === 'glow') {
    base.border = `1px solid color-mix(in srgb, ${glowColor} 55%, transparent)`;
    base.boxShadow = `0 0 0 1px color-mix(in srgb, ${glowColor} 20%, transparent), 0 0 28px color-mix(in srgb, ${glowColor} 28%, transparent), var(--inset-sheen)`;
  }
  return /*#__PURE__*/React.createElement("div", _extends({
    onClick: onClick,
    style: {
      ...base,
      ...style
    },
    onMouseEnter: e => {
      if (interactive || onClick) {
        e.currentTarget.style.transform = 'translateY(-2px)';
        e.currentTarget.style.borderColor = 'var(--border-strong)';
      }
    },
    onMouseLeave: e => {
      if (interactive || onClick) {
        e.currentTarget.style.transform = 'translateY(0)';
        e.currentTarget.style.borderColor = variant === 'glow' ? `color-mix(in srgb, ${glowColor} 55%, transparent)` : 'var(--border-default)';
      }
    }
  }, rest), children);
}
Object.assign(__ds_scope, { Card });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Card.jsx", error: String((e && e.message) || e) }); }

// components/core/Chip.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Awaken Chip — selectable token for onboarding choices, filters and tags.
 * Selected state lights up with the brand energy.
 */
function Chip({
  children,
  selected = false,
  icon = null,
  disabled = false,
  onClick,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("button", _extends({
    type: "button",
    disabled: disabled,
    onClick: onClick,
    "aria-pressed": selected,
    style: {
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
      ...style
    }
  }, rest), icon, children);
}
Object.assign(__ds_scope, { Chip });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Chip.jsx", error: String((e && e.message) || e) }); }

// components/core/Input.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Awaken Input — dark text field with label, optional leading icon, hint and error.
 */
function Input({
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
  const borderColor = error ? 'var(--danger)' : focused ? 'var(--border-focus)' : 'var(--border-default)';
  return /*#__PURE__*/React.createElement("label", {
    style: {
      display: 'block',
      ...style
    }
  }, label && /*#__PURE__*/React.createElement("span", {
    className: "eyebrow",
    style: {
      display: 'block',
      marginBottom: 8,
      color: 'var(--text-secondary)'
    }
  }, label), /*#__PURE__*/React.createElement("div", {
    style: {
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
      opacity: disabled ? 0.5 : 1
    }
  }, leftIcon && /*#__PURE__*/React.createElement("span", {
    style: {
      display: 'flex',
      color: 'var(--text-tertiary)'
    }
  }, leftIcon), /*#__PURE__*/React.createElement("input", _extends({
    type: type,
    value: value,
    onChange: onChange,
    placeholder: placeholder,
    disabled: disabled,
    onFocus: () => setFocused(true),
    onBlur: () => setFocused(false),
    style: {
      flex: 1,
      minWidth: 0,
      height: '100%',
      border: 'none',
      outline: 'none',
      background: 'transparent',
      color: 'var(--text-primary)',
      fontFamily: 'var(--font-body)',
      fontSize: 15
    }
  }, rest)), rightSlot), (hint || error) && /*#__PURE__*/React.createElement("span", {
    style: {
      display: 'block',
      marginTop: 6,
      fontSize: 12,
      color: error ? 'var(--danger)' : 'var(--text-tertiary)'
    }
  }, error || hint));
}
Object.assign(__ds_scope, { Input });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Input.jsx", error: String((e && e.message) || e) }); }

// components/core/Switch.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Awaken Switch — toggle for settings (notifications, premium features, units).
 */
function Switch({
  checked = false,
  onChange,
  disabled = false,
  label,
  style,
  ...rest
}) {
  const toggle = () => {
    if (!disabled && onChange) onChange(!checked);
  };
  const control = /*#__PURE__*/React.createElement("span", {
    role: "switch",
    "aria-checked": checked,
    onClick: toggle,
    style: {
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
      opacity: disabled ? 0.5 : 1
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      top: 3,
      left: checked ? 21 : 3,
      width: 20,
      height: 20,
      borderRadius: '50%',
      background: '#fff',
      boxShadow: '0 2px 4px rgba(0,0,0,0.4)',
      transition: 'left var(--dur-base) var(--ease-spring)'
    }
  }));
  if (!label) return React.cloneElement(control, {
    style: {
      ...control.props.style,
      ...style
    },
    ...rest
  });
  return /*#__PURE__*/React.createElement("label", _extends({
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: 12,
      cursor: disabled ? 'not-allowed' : 'pointer',
      ...style
    }
  }, rest), control, /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 15,
      color: 'var(--text-primary)'
    }
  }, label));
}
Object.assign(__ds_scope, { Switch });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Switch.jsx", error: String((e && e.message) || e) }); }

// components/game/ProgressRing.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Awaken ProgressRing — circular progress for daily goals (water, quests done, calories).
 * Defaults to the energy gradient via SVG stroke.
 */
function ProgressRing({
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
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      position: 'relative',
      width: size,
      height: size,
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("svg", {
    width: size,
    height: size,
    style: {
      transform: 'rotate(-90deg)'
    }
  }, /*#__PURE__*/React.createElement("defs", null, /*#__PURE__*/React.createElement("linearGradient", {
    id: gid,
    x1: "0%",
    y1: "0%",
    x2: "100%",
    y2: "100%"
  }, /*#__PURE__*/React.createElement("stop", {
    offset: "0%",
    stopColor: "#2D6FF5"
  }), /*#__PURE__*/React.createElement("stop", {
    offset: "100%",
    stopColor: "#8B3FD8"
  }))), /*#__PURE__*/React.createElement("circle", {
    cx: size / 2,
    cy: size / 2,
    r: r,
    fill: "none",
    stroke: trackColor,
    strokeWidth: stroke
  }), /*#__PURE__*/React.createElement("circle", {
    cx: size / 2,
    cy: size / 2,
    r: r,
    fill: "none",
    stroke: color === 'energy' ? `url(#${gid})` : color,
    strokeWidth: stroke,
    strokeLinecap: "round",
    strokeDasharray: circ,
    strokeDashoffset: circ * (1 - pct),
    style: {
      transition: 'stroke-dashoffset var(--dur-epic) var(--ease-out)',
      filter: 'drop-shadow(0 0 6px rgba(45,111,245,0.5))'
    }
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: 0,
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      textAlign: 'center'
    }
  }, children || /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("span", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: size * 0.26,
      color: 'var(--text-primary)',
      lineHeight: 1
    }
  }, label != null ? label : Math.round(pct * 100)), sublabel && /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 10,
      letterSpacing: '0.08em',
      color: 'var(--text-tertiary)',
      marginTop: 3,
      textTransform: 'uppercase'
    }
  }, sublabel))));
}
Object.assign(__ds_scope, { ProgressRing });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/game/ProgressRing.jsx", error: String((e && e.message) || e) }); }

// components/game/QuestCard.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const ATTR_TONE = {
  strength: 'red',
  agility: 'green',
  endurance: 'blue',
  vitality: 'gold',
  focus: 'purple',
  wisdom: 'cyan'
};
const ATTR_LABEL = {
  strength: 'Força',
  agility: 'Agilidade',
  endurance: 'Resistência',
  vitality: 'Vitalidade',
  focus: 'Foco',
  wisdom: 'Sabedoria'
};

/**
 * Awaken QuestCard — a daily quest row. The product's core loop unit: title, the
 * attribute it trains, the XP reward, and a completion state.
 */
function QuestCard({
  title,
  subtitle,
  xp = 0,
  attr,
  rewards,
  status = 'todo',
  icon = null,
  onToggle,
  onClick,
  style,
  ...rest
}) {
  const done = status === 'done';
  const active = status === 'active';

  // Attribute rewards: prefer the explicit `rewards` list (1-2 attrs w/ amounts);
  // fall back to a single `attr` tag (no amount) for back-compat.
  const rewardList = rewards && rewards.length ? rewards : attr ? [{
    attr,
    amount: null
  }] : [];
  return /*#__PURE__*/React.createElement("div", _extends({
    onClick: onClick,
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 14,
      padding: 14,
      clipPath: 'polygon(8px 0,100% 0,100% calc(100% - 8px),calc(100% - 8px) 100%,0 100%,0 8px)',
      background: active ? 'var(--grad-energy-soft), var(--bg-surface)' : 'var(--bg-surface)',
      border: active ? '1px solid rgba(77,139,255,0.5)' : '1px solid var(--border-default)',
      boxShadow: active ? '0 0 14px rgba(45,111,245,0.25)' : 'var(--shadow-sm)',
      opacity: done ? 0.55 : 1,
      cursor: onClick ? 'pointer' : 'default',
      transition: 'all var(--dur-base) var(--ease-out)',
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("button", {
    type: "button",
    onClick: e => {
      e.stopPropagation();
      onToggle && onToggle();
    },
    "aria-pressed": done,
    style: {
      flexShrink: 0,
      width: 26,
      height: 26,
      clipPath: 'polygon(5px 0,100% 0,100% calc(100% - 5px),calc(100% - 5px) 100%,0 100%,0 5px)',
      display: 'grid',
      placeItems: 'center',
      cursor: 'pointer',
      background: done ? 'var(--success)' : 'transparent',
      border: done ? '1.5px solid var(--success)' : '1.5px solid var(--border-strong)',
      color: '#fff',
      transition: 'all var(--dur-fast) var(--ease-out)',
      boxShadow: done ? '0 0 10px rgba(34,197,94,0.35)' : 'none'
    }
  }, done && /*#__PURE__*/React.createElement("svg", {
    width: "13",
    height: "13",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "3.5",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M20 6 9 17l-5-5"
  }))), icon && /*#__PURE__*/React.createElement("div", {
    style: {
      flexShrink: 0,
      width: 40,
      height: 40,
      clipPath: 'polygon(6px 0,100% 0,100% calc(100% - 6px),calc(100% - 6px) 100%,0 100%,0 6px)',
      display: 'grid',
      placeItems: 'center',
      background: 'var(--bg-elevated)',
      color: 'var(--blue-300)'
    }
  }, icon), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-body)',
      fontWeight: 600,
      fontSize: 15,
      color: 'var(--text-primary)',
      textDecoration: done ? 'line-through' : 'none'
    }
  }, title), subtitle && /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 13,
      color: 'var(--text-tertiary)',
      marginTop: 2,
      overflow: 'hidden',
      textOverflow: 'ellipsis',
      whiteSpace: 'nowrap'
    }
  }, subtitle), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexWrap: 'wrap',
      gap: 6,
      marginTop: 8
    }
  }, rewardList.map((r, i) => /*#__PURE__*/React.createElement(__ds_scope.Badge, {
    key: i,
    tone: ATTR_TONE[r.attr],
    variant: "soft"
  }, r.amount != null ? `+${r.amount} ` : '', ATTR_LABEL[r.attr])), active && /*#__PURE__*/React.createElement(__ds_scope.Badge, {
    tone: "cyan",
    variant: "outline"
  }, "Em andamento"))), /*#__PURE__*/React.createElement("div", {
    style: {
      flexShrink: 0,
      textAlign: 'right'
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 16,
      color: 'var(--gold-400)',
      lineHeight: 1
    }
  }, "+", xp), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 10,
      letterSpacing: '0.1em',
      color: 'var(--text-tertiary)',
      marginTop: 3
    }
  }, "XP")));
}
Object.assign(__ds_scope, { QuestCard });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/game/QuestCard.jsx", error: String((e && e.message) || e) }); }

// components/game/RankBadge.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const RANKS = {
  E: {
    color: 'var(--rank-e)',
    grad: 'linear-gradient(160deg, #8A92A3, #4B5160)'
  },
  D: {
    color: 'var(--rank-d)',
    grad: 'linear-gradient(160deg, #4CE07F, #15803D)'
  },
  C: {
    color: 'var(--rank-c)',
    grad: 'linear-gradient(160deg, #5B9BFF, #1D4ED8)'
  },
  B: {
    color: 'var(--rank-b)',
    grad: 'linear-gradient(160deg, #C07BFF, #7E22CE)'
  },
  A: {
    color: 'var(--rank-a)',
    grad: 'linear-gradient(160deg, #FACC15, #B8860B)'
  },
  S: {
    color: 'var(--rank-s)',
    grad: 'linear-gradient(160deg, #FF6B5B, #C81E2C)'
  },
  SS: {
    color: 'var(--rank-ss)',
    grad: 'var(--grad-rank-ss)'
  },
  SSS: {
    color: 'var(--rank-sss)',
    grad: 'var(--grad-rank-sss)'
  }
};
const DOT_SIZE = 0.09; // corner dot relative to size

/**
 * Awaken RankBadge — the hexagonal rank emblem (E → SSS). The core gamification motif:
 * a faceted shield that glows in the rank's color.
 */
function RankBadge({
  rank = 'E',
  size = 64,
  glow = true,
  style,
  ...rest
}) {
  const r = RANKS[rank] || RANKS.E;
  const d = size * DOT_SIZE;
  const inset = size * 0.06;
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      position: 'relative',
      width: size,
      height: size,
      display: 'inline-flex',
      alignItems: 'center',
      justifyContent: 'center',
      filter: glow ? `drop-shadow(0 0 ${size * 0.22}px color-mix(in srgb, ${r.color} 65%, transparent))` : 'none',
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: 0,
      background: r.grad,
      transform: 'rotate(45deg)'
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset,
      background: 'var(--bg-base)',
      transform: 'rotate(45deg)'
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset,
      background: `linear-gradient(180deg, color-mix(in srgb, ${r.color} 18%, transparent), transparent 55%)`,
      transform: 'rotate(45deg)'
    }
  }), [{
    top: -d / 2,
    left: -d / 2
  }, {
    top: -d / 2,
    left: size - d / 2
  }, {
    top: size - d / 2,
    left: -d / 2
  }, {
    top: size - d / 2,
    left: size - d / 2
  }].map((pos, i) => /*#__PURE__*/React.createElement("span", {
    key: i,
    style: {
      position: 'absolute',
      ...pos,
      width: d,
      height: d,
      background: r.color,
      transform: 'rotate(45deg)',
      boxShadow: `0 0 ${d * 2}px ${r.color}`
    }
  })), /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'relative',
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: size * (rank.length === 3 ? 0.28 : rank.length === 2 ? 0.36 : 0.46),
      letterSpacing: '-0.01em',
      lineHeight: 1,
      color: r.color,
      textShadow: `0 0 ${size * 0.14}px color-mix(in srgb, ${r.color} 80%, transparent)`
    }
  }, rank));
}
Object.assign(__ds_scope, { RankBadge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/game/RankBadge.jsx", error: String((e && e.message) || e) }); }

// components/game/StatBar.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const ATTRS = {
  strength: {
    label: 'Força',
    color: 'var(--attr-strength)'
  },
  agility: {
    label: 'Agilidade',
    color: 'var(--attr-agility)'
  },
  endurance: {
    label: 'Resistência',
    color: 'var(--attr-endurance)'
  },
  vitality: {
    label: 'Vitalidade',
    color: 'var(--attr-vitality)'
  },
  focus: {
    label: 'Foco',
    color: 'var(--attr-focus)'
  },
  wisdom: {
    label: 'Sabedoria',
    color: 'var(--attr-wisdom)'
  }
};

/**
 * Awaken StatBar — a single character attribute (Força, Agilidade, …) as an RPG
 * status bar. Color is fixed per attribute.
 */
function StatBar({
  attr = 'strength',
  value = 0,
  max = 100,
  label,
  style,
  ...rest
}) {
  const a = ATTRS[attr] || ATTRS.strength;
  const pct = Math.max(0, Math.min(100, value / max * 100));
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'baseline',
      justifyContent: 'space-between',
      marginBottom: 6
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 12,
      fontWeight: 600,
      letterSpacing: '0.08em',
      textTransform: 'uppercase',
      color: 'var(--text-secondary)'
    }
  }, label || a.label), /*#__PURE__*/React.createElement("span", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 13,
      fontWeight: 700,
      color: a.color
    }
  }, Math.round(value))), /*#__PURE__*/React.createElement("div", {
    style: {
      height: 8,
      borderRadius: 'var(--radius-pill)',
      background: 'var(--ink-700)',
      overflow: 'hidden',
      boxShadow: 'inset 0 1px 2px rgba(0,0,0,0.5)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      height: '100%',
      width: `${pct}%`,
      borderRadius: 'var(--radius-pill)',
      background: `linear-gradient(90deg, color-mix(in srgb, ${a.color} 70%, #000), ${a.color})`,
      boxShadow: `0 0 10px color-mix(in srgb, ${a.color} 55%, transparent)`,
      transition: 'width var(--dur-slow) var(--ease-out)'
    }
  })));
}
Object.assign(__ds_scope, { StatBar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/game/StatBar.jsx", error: String((e && e.message) || e) }); }

// components/game/SystemWindow.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/* Quest taxonomy → theme. Daily=blue, Dungeon=purple, Raid=red/gold. */
const KINDS = {
  daily: {
    label: 'Quest Diária',
    color: 'var(--blue-400)',
    rgb: '77,139,255',
    glow: 'var(--glow-blue)'
  },
  dungeon: {
    label: 'Dungeon',
    color: 'var(--purple-400)',
    rgb: '166,92,238',
    glow: 'var(--glow-purple)'
  },
  raid: {
    label: 'Raid',
    color: 'var(--red-500, #EF4444)',
    rgb: '239,68,68',
    glow: 'var(--glow-danger)'
  }
};
const ATTRS = {
  strength: {
    label: 'Força',
    color: 'var(--attr-strength)'
  },
  agility: {
    label: 'Agilidade',
    color: 'var(--attr-agility)'
  },
  endurance: {
    label: 'Resistência',
    color: 'var(--attr-endurance)'
  },
  vitality: {
    label: 'Vitalidade',
    color: 'var(--attr-vitality)'
  },
  focus: {
    label: 'Foco',
    color: 'var(--attr-focus)'
  },
  wisdom: {
    label: 'Sabedoria',
    color: 'var(--attr-wisdom)'
  }
};

/* Notched / faceted octagon — the HUD signature. */
const facet = n => `polygon(${n}px 0, calc(100% - ${n}px) 0, 100% ${n}px, 100% calc(100% - ${n}px), calc(100% - ${n}px) 100%, ${n}px 100%, 0 calc(100% - ${n}px), 0 ${n}px)`;
function SectionLabel({
  children,
  color
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10,
      margin: '0 0 11px'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: 6,
      height: 6,
      background: color,
      transform: 'rotate(45deg)',
      flex: 'none'
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 11,
      fontWeight: 700,
      letterSpacing: '0.18em',
      textTransform: 'uppercase',
      color
    }
  }, children), /*#__PURE__*/React.createElement("span", {
    style: {
      flex: 1,
      height: 1,
      background: `linear-gradient(90deg, color-mix(in srgb, ${color} 40%, transparent), transparent)`
    }
  }));
}

/**
 * Awaken SystemWindow — the "System" HUD panel that announces a quest. A faceted,
 * glowing window themed by quest kind (daily / dungeon / raid) showing goals,
 * rewards (XP + 1-2 attribute points) and, for daily quests, a penalty warning.
 */
function SystemWindow({
  kind = 'daily',
  title,
  description,
  rank,
  goals = [],
  xp = 0,
  rewards = [],
  warning,
  participants,
  cta,
  onCta,
  style,
  ...rest
}) {
  const k = KINDS[kind] || KINDS.daily;
  const showWarning = kind === 'daily' && warning !== false;
  const warnText = typeof warning === 'string' ? warning : 'A recompensa diária é concedida apenas ao completar todas as metas. Quests diárias não cumpridas reiniciam a sua ofensiva (streak).';
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      position: 'relative',
      width: '100%',
      maxWidth: 420,
      fontFamily: 'var(--font-body)',
      filter: `drop-shadow(0 0 30px rgba(${k.rgb},0.28))`,
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: 0,
      clipPath: facet(16),
      background: `linear-gradient(150deg, ${k.color}, rgba(${k.rgb},0.15) 45%, ${k.color})`
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      clipPath: facet(15),
      margin: 1.5,
      background: `linear-gradient(180deg, color-mix(in srgb, ${k.color} 9%, var(--bg-surface)), var(--bg-base))`,
      padding: '20px 22px 22px',
      boxShadow: 'var(--inset-sheen)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      top: 8,
      left: 8,
      width: 14,
      height: 14,
      borderTop: `2px solid ${k.color}`,
      borderLeft: `2px solid ${k.color}`,
      opacity: 0.7
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      top: 8,
      right: 8,
      width: 14,
      height: 14,
      borderTop: `2px solid ${k.color}`,
      borderRight: `2px solid ${k.color}`,
      opacity: 0.7
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: 8,
      height: 26,
      padding: '0 16px',
      clipPath: facet(7),
      background: `rgba(${k.rgb},0.16)`,
      border: `1px solid color-mix(in srgb, ${k.color} 55%, transparent)`
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: 5,
      height: 5,
      background: k.color,
      transform: 'rotate(45deg)',
      boxShadow: `0 0 8px ${k.color}`
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 12,
      fontWeight: 700,
      letterSpacing: '0.2em',
      textTransform: 'uppercase',
      color: k.color
    }
  }, k.label))), /*#__PURE__*/React.createElement("div", {
    style: {
      textAlign: 'center',
      marginBottom: description ? 6 : 18
    }
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      margin: 0,
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 23,
      lineHeight: 1.12,
      color: 'var(--text-primary)',
      textShadow: `0 0 18px rgba(${k.rgb},0.25)`
    }
  }, title)), description && /*#__PURE__*/React.createElement("p", {
    style: {
      margin: '0 0 18px',
      textAlign: 'center',
      fontSize: 13,
      lineHeight: 1.5,
      color: 'var(--text-tertiary)'
    }
  }, description), rank && /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'center',
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 11,
      fontWeight: 600,
      letterSpacing: '0.12em',
      textTransform: 'uppercase',
      color: 'var(--text-tertiary)'
    }
  }, "Dificuldade\xA0\xB7\xA0", /*#__PURE__*/React.createElement("span", {
    style: {
      color: k.color,
      fontWeight: 700
    }
  }, "Rank ", rank))), goals.length > 0 && /*#__PURE__*/React.createElement("div", {
    style: {
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement(SectionLabel, {
    color: k.color
  }, "Goal"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 11
    }
  }, goals.map((g, i) => {
    const pct = g.target ? Math.min(100, Math.round(g.current / g.target * 100)) : g.done ? 100 : 0;
    const done = pct >= 100;
    return /*#__PURE__*/React.createElement("div", {
      key: i
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        alignItems: 'baseline',
        justifyContent: 'space-between',
        marginBottom: 6
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        fontSize: 14,
        color: done ? 'var(--text-secondary)' : 'var(--text-primary)'
      }
    }, done && /*#__PURE__*/React.createElement("svg", {
      width: "14",
      height: "14",
      viewBox: "0 0 24 24",
      fill: "none",
      stroke: "var(--success)",
      strokeWidth: "3",
      strokeLinecap: "round",
      strokeLinejoin: "round"
    }, /*#__PURE__*/React.createElement("path", {
      d: "M20 6 9 17l-5-5"
    })), g.label), /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: 'var(--font-display)',
        fontSize: 13,
        fontWeight: 600,
        fontVariantNumeric: 'tabular-nums',
        color: done ? 'var(--success)' : k.color
      }
    }, g.target != null ? `${g.current}/${g.target}` : done ? 'OK' : '—', g.unit ? ` ${g.unit}` : '')), /*#__PURE__*/React.createElement("div", {
      style: {
        height: 4,
        borderRadius: 999,
        background: 'rgba(255,255,255,0.07)',
        overflow: 'hidden'
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        width: `${pct}%`,
        height: '100%',
        borderRadius: 999,
        background: done ? 'var(--success)' : k.color,
        boxShadow: done ? 'none' : `0 0 8px ${k.color}`
      }
    })));
  }))), (xp > 0 || rewards.length > 0) && /*#__PURE__*/React.createElement("div", {
    style: {
      marginBottom: showWarning || cta ? 18 : 0
    }
  }, /*#__PURE__*/React.createElement(SectionLabel, {
    color: k.color
  }, "Recompensa"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexWrap: 'wrap',
      gap: 8
    }
  }, xp > 0 && /*#__PURE__*/React.createElement("span", {
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: 6,
      height: 30,
      padding: '0 12px',
      clipPath: facet(6),
      background: 'rgba(245,197,24,0.12)',
      border: '1px solid color-mix(in srgb, var(--gold-500) 45%, transparent)',
      fontFamily: 'var(--font-display)',
      fontSize: 13,
      fontWeight: 700,
      color: 'var(--gold-400)',
      fontVariantNumeric: 'tabular-nums'
    }
  }, /*#__PURE__*/React.createElement("svg", {
    width: "13",
    height: "13",
    viewBox: "0 0 24 24",
    fill: "var(--gold-400)",
    stroke: "none"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M13 2 3 14h7l-1 8 10-12h-7z"
  })), "+", xp, " XP"), rewards.map((r, i) => {
    const a = ATTRS[r.attr] || ATTRS.strength;
    return /*#__PURE__*/React.createElement("span", {
      key: i,
      style: {
        display: 'inline-flex',
        alignItems: 'center',
        gap: 6,
        height: 30,
        padding: '0 12px',
        clipPath: facet(6),
        background: `color-mix(in srgb, ${a.color} 13%, transparent)`,
        border: `1px solid color-mix(in srgb, ${a.color} 45%, transparent)`,
        fontFamily: 'var(--font-display)',
        fontSize: 13,
        fontWeight: 700,
        color: a.color,
        fontVariantNumeric: 'tabular-nums'
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: 6,
        height: 6,
        background: a.color,
        transform: 'rotate(45deg)'
      }
    }), "+", r.amount, " ", a.label);
  }))), participants && /*#__PURE__*/React.createElement("div", {
    style: {
      marginBottom: showWarning || cta ? 18 : 0
    }
  }, /*#__PURE__*/React.createElement(SectionLabel, {
    color: k.color
  }, "Esquadr\xE3o"), /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 13,
      color: 'var(--text-secondary)',
      fontVariantNumeric: 'tabular-nums'
    }
  }, participants.current, "/", participants.max, " ca\xE7adores reunidos")), showWarning && /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'flex-start',
      gap: 10,
      padding: '12px 13px',
      clipPath: facet(8),
      background: 'rgba(239,68,68,0.08)',
      border: '1px solid color-mix(in srgb, var(--danger) 38%, transparent)'
    }
  }, /*#__PURE__*/React.createElement("svg", {
    width: "16",
    height: "16",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "var(--danger)",
    strokeWidth: "2.2",
    strokeLinecap: "round",
    strokeLinejoin: "round",
    style: {
      flex: 'none',
      marginTop: 1
    }
  }, /*#__PURE__*/React.createElement("path", {
    d: "m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M12 9v4"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M12 17h.01"
  })), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 11,
      fontWeight: 700,
      letterSpacing: '0.16em',
      textTransform: 'uppercase',
      color: 'var(--danger)',
      marginBottom: 3
    }
  }, "Aviso"), /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 0,
      fontSize: 12.5,
      lineHeight: 1.45,
      color: 'var(--text-secondary)'
    }
  }, warnText))), cta && /*#__PURE__*/React.createElement("button", {
    onClick: onCta,
    style: {
      marginTop: 18,
      width: '100%',
      height: 46,
      clipPath: facet(8),
      cursor: 'pointer',
      fontFamily: 'var(--font-display)',
      fontSize: 14,
      fontWeight: 700,
      letterSpacing: '0.08em',
      textTransform: 'uppercase',
      color: 'var(--text-on-primary)',
      border: 'none',
      background: `linear-gradient(180deg, ${k.color}, color-mix(in srgb, ${k.color} 70%, #000))`,
      boxShadow: k.glow
    }
  }, cta)));
}
Object.assign(__ds_scope, { SystemWindow });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/game/SystemWindow.jsx", error: String((e && e.message) || e) }); }

// components/game/XPBar.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Awaken XPBar — level + experience progress. Gold→amber fill with a moving sheen.
 */
function XPBar({
  value = 0,
  max = 100,
  level,
  showValues = true,
  height = 12,
  style,
  ...rest
}) {
  const pct = Math.max(0, Math.min(100, value / max * 100));
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      ...style
    }
  }, rest), (level != null || showValues) && /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'baseline',
      justifyContent: 'space-between',
      marginBottom: 7
    }
  }, level != null && /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 600,
      fontSize: 13,
      letterSpacing: '0.1em',
      textTransform: 'uppercase',
      color: 'var(--gold-400)'
    }
  }, "N\xEDvel ", level), showValues && /*#__PURE__*/React.createElement("span", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 12,
      color: 'var(--text-tertiary)'
    }
  }, Math.round(value), " / ", max, " XP")), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      height,
      borderRadius: 'var(--radius-pill)',
      background: 'var(--ink-700)',
      overflow: 'hidden',
      boxShadow: 'inset 0 1px 2px rgba(0,0,0,0.5)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: 0,
      width: `${pct}%`,
      borderRadius: 'var(--radius-pill)',
      background: 'var(--grad-xp)',
      boxShadow: '0 0 14px color-mix(in srgb, var(--gold-500) 55%, transparent)',
      transition: 'width var(--dur-epic) var(--ease-out)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      inset: 0,
      background: 'linear-gradient(180deg, rgba(255,255,255,0.45), transparent 60%)',
      borderRadius: 'inherit'
    }
  }))));
}
Object.assign(__ds_scope, { XPBar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/game/XPBar.jsx", error: String((e && e.message) || e) }); }

// player-screen/app.jsx
try { (() => {
// ═══════════════════════════════════════════════════════════════
// Awaken — Player Screen App
// Screens: Home · Perfil · Inventário · Loja · Config + Quest Detail
// ═══════════════════════════════════════════════════════════════

// ─────────────────────────── DATA ───────────────────────────────

const PLAYER = {
  name: 'Vinícius Ottoni',
  initials: 'VO',
  level: 37,
  rank: 'B',
  className: 'STRIKER',
  xp: 648,
  xpMax: 900,
  xpToNext: 260,
  streakDays: 12,
  notifications: 3,
  gold: 2840,
  gems: 42,
  stats: {
    strength: 72,
    agility: 58,
    endurance: 65,
    vitality: 80,
    focus: 45,
    wisdom: 50
  },
  onboarding: {
    goal: 'Ganhar massa muscular',
    fitnessLevel: 'Intermediário',
    trainingDays: ['Seg', 'Qua', 'Sex', 'Sáb'],
    age: 28,
    weight: '82 kg',
    height: '178 cm'
  }
};
const QUESTS = [{
  id: 'daily',
  type: 'daily',
  title: 'Quest Diária',
  completedCount: 2,
  totalCount: 4,
  xpReward: 120,
  exercises: [{
    id: 1,
    name: 'Flexão de Braço',
    detail: '3 séries × 20 reps',
    status: 'done',
    attr: 'strength'
  }, {
    id: 2,
    name: 'Agachamento',
    detail: '4 séries × 15 reps',
    status: 'done',
    attr: 'agility'
  }, {
    id: 3,
    name: 'Prancha',
    detail: '3 séries × 60s',
    status: 'active',
    attr: 'endurance',
    activeSets: 2,
    totalSets: 3
  }, {
    id: 4,
    name: 'Corrida',
    detail: '5 km',
    status: 'todo',
    attr: 'vitality'
  }]
}, {
  id: 'raid',
  type: 'raid',
  title: 'Raid (nacional)',
  completedCount: 8,
  totalCount: 20,
  xpReward: 500,
  participants: 1247,
  exercises: [{
    id: 1,
    name: 'Burpees',
    detail: 'Meta coletiva: 10.000',
    current: 6800,
    total: 10000,
    unit: 'reps',
    attr: 'strength'
  }, {
    id: 2,
    name: 'Corrida',
    detail: 'Meta coletiva: 5.000',
    current: 3200,
    total: 5000,
    unit: 'km',
    attr: 'agility'
  }, {
    id: 3,
    name: 'Prancha',
    detail: 'Meta coletiva: 500',
    current: 280,
    total: 500,
    unit: 'min',
    attr: 'endurance'
  }]
}];
const INVENTORY = [{
  id: 1,
  name: 'Espada de Ferro',
  rarity: 'common',
  type: 'Arma',
  icon: '⚔️',
  desc: 'Espada forjada em ferro puro. +5 Força durante treinos de musculação.'
}, {
  id: 2,
  name: 'Orbe de Mana',
  rarity: 'uncommon',
  type: 'Acessório',
  icon: '🔮',
  desc: 'Orbe pulsante com energia azul. +8 Foco e reduz fadiga mental após o treino.'
}, {
  id: 3,
  name: 'Armadura de Couro',
  rarity: 'common',
  type: 'Armadura',
  icon: '🛡️',
  desc: 'Armadura leve de couro de monstro. +4 Resistência.'
}, {
  id: 4,
  name: 'Poção de Cura',
  rarity: 'consumable',
  type: 'Consumível',
  icon: '🧪',
  desc: 'Recuperação 20% mais rápida após treinos intensos.',
  qty: 3
}, {
  id: 5,
  name: 'Fragmento de Cristal',
  rarity: 'rare',
  type: 'Material',
  icon: '💎',
  desc: 'Fragmento de cristal de dungeon S. Usado para forjar itens épicos.',
  qty: 2
}, {
  id: 6,
  name: 'Gema de Vitalidade',
  rarity: 'rare',
  type: 'Gema',
  icon: '💠',
  desc: 'Gema de energia vital. Encanta equipamentos com +12 Vitalidade.'
}, {
  id: 7,
  name: 'Manto das Sombras',
  rarity: 'epic',
  type: 'Armadura',
  icon: '🌑',
  desc: 'Manto épico das sombras. +15 Agilidade e +10 Foco.'
}, {
  id: 8,
  name: 'Pergaminho de Técnica',
  rarity: 'uncommon',
  type: 'Pergaminho',
  icon: '📜',
  desc: 'Desbloqueia nova técnica de treino do seu estilo de combate.',
  qty: 1
}, {
  id: 9,
  name: 'Escudo do Guerreiro',
  rarity: 'uncommon',
  type: 'Armadura',
  icon: '🛡️',
  desc: '+6 Resistência e +3 Vitalidade. Forjado após Raid nacional.'
}];
const SHOP_ITEMS = [{
  id: 1,
  name: 'Poção de XP Duplo',
  rarity: 'uncommon',
  icon: '⚗️',
  desc: 'Dobra o XP ganho por 24 horas.',
  priceGold: 350
}, {
  id: 2,
  name: 'Título: Elite',
  rarity: 'epic',
  icon: '👑',
  desc: 'Título cosmético exclusivo.',
  priceGems: 800
}, {
  id: 3,
  name: 'Caixa de Raid',
  rarity: 'rare',
  icon: '📦',
  desc: 'Contém itens raros de dungeon.',
  priceGold: 600
}, {
  id: 4,
  name: '+10 Slots Inv.',
  rarity: 'common',
  icon: '🎒',
  desc: 'Expande o inventário em 10 slots.',
  priceGems: 500
}];

// ──────────────────── SHARED HELPERS ────────────────────────────

const CP = 'polygon(10px 0%,100% 0%,100% calc(100% - 10px),calc(100% - 10px) 100%,0% 100%,0% 10px)';
const CP_SM = 'polygon(6px 0%,100% 0%,100% calc(100% - 6px),calc(100% - 6px) 100%,0% 100%,0% 6px)';
function Tag({
  children,
  color = '#828AAE'
}) {
  return /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 10,
      fontWeight: 700,
      letterSpacing: '0.08em',
      textTransform: 'uppercase',
      color,
      background: `color-mix(in srgb,${color} 16%,transparent)`,
      border: `1px solid color-mix(in srgb,${color} 35%,transparent)`,
      borderRadius: 4,
      padding: '2px 7px'
    }
  }, children);
}
function SectionLabel({
  children,
  right
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 12
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 11,
      fontWeight: 600,
      letterSpacing: '0.12em',
      textTransform: 'uppercase',
      color: '#5E6488'
    }
  }, children), right && /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 12,
      color: '#5E6488'
    }
  }, right));
}
function ACard({
  children,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      background: '#111320',
      border: '1px solid rgba(255,255,255,0.08)',
      clipPath: CP,
      boxShadow: '0 6px 18px rgba(0,0,0,0.45),inset 0 1px 0 rgba(255,255,255,0.04)',
      ...(style || {})
    }
  }, children);
}
function AttrTag({
  attr
}) {
  const a = ATTR_CONFIG[attr];
  if (!a) return null;
  return /*#__PURE__*/React.createElement(Tag, {
    color: a.color
  }, a.label);
}
function Screen({
  children
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      overflowY: 'auto',
      WebkitOverflowScrolling: 'touch',
      padding: '20px 20px 16px',
      animation: 'fadeSlideIn 240ms cubic-bezier(0.16,1,0.3,1)'
    }
  }, children);
}

// ──────────────────────── HOME ───────────────────────────────────

function HomeScreen({
  onQuest,
  onProfile
}) {
  return /*#__PURE__*/React.createElement(Screen, null, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 12,
      marginBottom: 20
    }
  }, /*#__PURE__*/React.createElement("div", {
    onClick: onProfile,
    style: {
      width: 52,
      height: 52,
      borderRadius: '50%',
      flexShrink: 0,
      background: 'linear-gradient(135deg,#2D6FF5,#8B3FD8)',
      border: '2.5px solid #A855F7',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      fontSize: 17,
      color: '#fff',
      cursor: 'pointer',
      boxShadow: '0 0 20px rgba(139,63,216,0.45)'
    }
  }, "VO"), /*#__PURE__*/React.createElement("div", {
    onClick: onProfile,
    style: {
      flex: 1,
      cursor: 'pointer'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 10,
      letterSpacing: '0.14em',
      textTransform: 'uppercase',
      color: '#5E6488',
      marginBottom: 2
    }
  }, "BOM TREINO"), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 18,
      fontWeight: 700,
      color: '#F2F5FF',
      letterSpacing: '-0.01em',
      lineHeight: 1.15
    }
  }, "Vin\xEDcius Ottoni")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 8
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative'
    }
  }, /*#__PURE__*/React.createElement("button", {
    style: {
      width: 40,
      height: 40,
      background: '#161929',
      border: '1px solid rgba(255,255,255,0.09)',
      borderRadius: 8,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      cursor: 'pointer'
    }
  }, /*#__PURE__*/React.createElement("svg", {
    width: "17",
    height: "17",
    viewBox: "0 0 24 24",
    fill: "#FF9500"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M13.5.67s.74 2.65.74 4.8c0 2.06-1.35 3.73-3.41 3.73-2.07 0-3.63-1.67-3.63-3.73l.03-.36C5.21 7.51 4 10.62 4 14c0 4.42 3.58 8 8 8s8-3.58 8-8C20 8.61 17.41 3.8 13.5.67z"
  }))), /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      top: -5,
      right: -5,
      background: '#FF9500',
      color: '#fff',
      fontSize: 9,
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      borderRadius: 999,
      padding: '1px 5px',
      lineHeight: '16px',
      minWidth: 16,
      textAlign: 'center'
    }
  }, PLAYER.streakDays)), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative'
    }
  }, /*#__PURE__*/React.createElement("button", {
    style: {
      width: 40,
      height: 40,
      background: '#161929',
      border: '1px solid rgba(255,255,255,0.09)',
      borderRadius: 8,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      cursor: 'pointer'
    }
  }, /*#__PURE__*/React.createElement("svg", {
    width: "17",
    height: "17",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "#AEB4D0",
    strokeWidth: "2",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M13.73 21a2 2 0 01-3.46 0"
  }))), /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      top: -5,
      right: -5,
      background: '#EF4444',
      color: '#fff',
      fontSize: 9,
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      borderRadius: 999,
      padding: '1px 5px',
      lineHeight: '16px',
      minWidth: 16,
      textAlign: 'center'
    }
  }, PLAYER.notifications)))), /*#__PURE__*/React.createElement(ACard, {
    style: {
      padding: 20,
      marginBottom: 22
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'flex-start',
      gap: 16,
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement(RankBadge, {
    rank: PLAYER.rank,
    size: 72
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 8,
      marginBottom: 5
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      fontSize: 22,
      color: '#F2F5FF'
    }
  }, "Rank ", PLAYER.rank), /*#__PURE__*/React.createElement(Tag, {
    color: "#A855F7"
  }, PLAYER.className)), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Sora',sans-serif",
      fontSize: 13,
      color: '#828AAE',
      lineHeight: 1.4
    }
  }, "Faltam ", PLAYER.xpToNext, " XP para o pr\xF3ximo n\xEDvel"))), /*#__PURE__*/React.createElement(XPBar, {
    value: PLAYER.xp,
    max: PLAYER.xpMax,
    level: PLAYER.level,
    height: 12
  })), /*#__PURE__*/React.createElement(SectionLabel, {
    right: `1/${QUESTS.length}`
  }, "Lista de Quests"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 10
    }
  }, QUESTS.map(q => /*#__PURE__*/React.createElement("button", {
    key: q.id,
    onClick: () => onQuest(q.id),
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '16px 18px',
      background: '#111320',
      border: '1px solid rgba(255,255,255,0.08)',
      clipPath: CP_SM,
      cursor: 'pointer',
      textAlign: 'left',
      transition: 'background 140ms'
    }
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 600,
      fontSize: 18,
      color: '#F2F5FF'
    }
  }, q.title), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 13,
      color: '#828AAE',
      marginLeft: 10
    }
  }, q.completedCount, "/", q.totalCount)), /*#__PURE__*/React.createElement("svg", {
    width: "18",
    height: "18",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "#5E6488",
    strokeWidth: "2.5",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M5 12h14M12 5l7 7-7 7"
  }))))));
}

// ─────────────────── QUEST DETAIL ───────────────────────────────

function DailyRow({
  ex
}) {
  const sc = ex.status === 'done' ? '#22C55E' : ex.status === 'active' ? '#2D6FF5' : '#474C66';
  return /*#__PURE__*/React.createElement(ACard, {
    style: {
      padding: '14px 16px',
      opacity: ex.status === 'todo' ? 0.5 : 1
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 12
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      width: 28,
      height: 28,
      borderRadius: 4,
      flexShrink: 0,
      background: `color-mix(in srgb,${sc} 14%,transparent)`,
      border: `1.5px solid ${sc}`,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      boxShadow: ex.status === 'active' ? '0 0 10px rgba(45,111,245,0.3)' : 'none'
    }
  }, ex.status === 'done' && /*#__PURE__*/React.createElement("svg", {
    width: "13",
    height: "13",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "#22C55E",
    strokeWidth: "3",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M20 6 9 17l-5-5"
  })), ex.status === 'active' && /*#__PURE__*/React.createElement("div", {
    style: {
      width: 8,
      height: 8,
      borderRadius: '50%',
      background: '#2D6FF5',
      animation: 'pulse 1.5s ease-in-out infinite'
    }
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Sora',sans-serif",
      fontWeight: 600,
      fontSize: 14,
      color: ex.status === 'done' ? '#5E6488' : '#F2F5FF',
      textDecoration: ex.status === 'done' ? 'line-through' : 'none',
      marginBottom: 3
    }
  }, ex.name), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 11,
      color: '#828AAE'
    }
  }, ex.detail), ex.status === 'active' && ex.totalSets && /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 7
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 4,
      marginBottom: 3
    }
  }, Array.from({
    length: ex.totalSets
  }).map((_, i) => /*#__PURE__*/React.createElement("div", {
    key: i,
    style: {
      flex: 1,
      height: 4,
      borderRadius: 999,
      background: i < ex.activeSets ? '#2D6FF5' : '#1D2133',
      boxShadow: i < ex.activeSets ? '0 0 6px rgba(45,111,245,0.5)' : 'none',
      transition: 'background 300ms'
    }
  }))), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 10,
      color: '#5E6488'
    }
  }, ex.activeSets, "/", ex.totalSets, " s\xE9ries"))), /*#__PURE__*/React.createElement(AttrTag, {
    attr: ex.attr
  })));
}
function RaidRow({
  ex
}) {
  const a = ATTR_CONFIG[ex.attr] || {
    color: '#828AAE'
  };
  const pct = Math.round(ex.current / ex.total * 100);
  return /*#__PURE__*/React.createElement(ACard, {
    style: {
      padding: '14px 16px'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 10
    }
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Sora',sans-serif",
      fontWeight: 600,
      fontSize: 14,
      color: '#F2F5FF'
    }
  }, ex.name), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Sora',sans-serif",
      fontSize: 12,
      color: '#5E6488',
      marginLeft: 8
    }
  }, ex.detail)), /*#__PURE__*/React.createElement(AttrTag, {
    attr: ex.attr
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      height: 8,
      borderRadius: 999,
      background: '#1D2133',
      overflow: 'hidden',
      marginBottom: 5
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      height: '100%',
      width: `${pct}%`,
      borderRadius: 999,
      background: `linear-gradient(90deg,color-mix(in srgb,${a.color} 70%,#000),${a.color})`,
      boxShadow: `0 0 8px color-mix(in srgb,${a.color} 55%,transparent)`,
      transition: 'width 600ms cubic-bezier(0.16,1,0.3,1)'
    }
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 11,
      color: '#828AAE'
    }
  }, ex.current.toLocaleString('pt-BR'), " / ", ex.total.toLocaleString('pt-BR'), " ", ex.unit), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 11,
      fontWeight: 700,
      color: a.color
    }
  }, pct, "%")));
}
function QuestDetailScreen({
  quest,
  onBack
}) {
  if (!quest) return null;
  const isRaid = quest.type === 'raid';
  const progress = quest.completedCount / quest.totalCount * 100;
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      overflow: 'hidden',
      animation: 'fadeSlideIn 240ms cubic-bezier(0.16,1,0.3,1)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 12,
      padding: '14px 20px',
      borderBottom: '1px solid rgba(255,255,255,0.07)',
      background: '#0A0B12',
      flexShrink: 0
    }
  }, /*#__PURE__*/React.createElement("button", {
    onClick: onBack,
    style: {
      width: 36,
      height: 36,
      background: '#161929',
      border: '1px solid rgba(255,255,255,0.09)',
      borderRadius: 6,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      cursor: 'pointer',
      flexShrink: 0
    }
  }, /*#__PURE__*/React.createElement("svg", {
    width: "18",
    height: "18",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "#AEB4D0",
    strokeWidth: "2.5",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M19 12H5M12 19l-7-7 7-7"
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      fontSize: 16,
      color: '#F2F5FF'
    }
  }, quest.title), isRaid && /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Sora',sans-serif",
      fontSize: 11,
      color: '#5E6488',
      marginTop: 1
    }
  }, quest.participants?.toLocaleString('pt-BR'), " participantes ativos")), /*#__PURE__*/React.createElement("div", {
    style: {
      textAlign: 'right'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      fontSize: 12,
      letterSpacing: '0.08em',
      color: '#FFD64A'
    }
  }, "+", quest.xpReward, " XP"), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 11,
      color: '#5E6488',
      marginTop: 1
    }
  }, quest.completedCount, "/", quest.totalCount))), /*#__PURE__*/React.createElement("div", {
    style: {
      padding: '10px 20px 0',
      flexShrink: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      height: 4,
      borderRadius: 999,
      background: '#1D2133',
      overflow: 'hidden'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      height: '100%',
      width: `${progress}%`,
      borderRadius: 999,
      background: 'linear-gradient(90deg,#2D6FF5,#8B3FD8)',
      transition: 'width 600ms cubic-bezier(0.16,1,0.3,1)'
    }
  }))), /*#__PURE__*/React.createElement(Screen, null, /*#__PURE__*/React.createElement(SectionLabel, null, isRaid ? 'Progresso Coletivo' : 'Exercícios'), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 10
    }
  }, quest.exercises.map(ex => isRaid ? /*#__PURE__*/React.createElement(RaidRow, {
    key: ex.id,
    ex: ex
  }) : /*#__PURE__*/React.createElement(DailyRow, {
    key: ex.id,
    ex: ex
  })))));
}

// ─────────────────────── PERFIL ─────────────────────────────────

function ProfileScreen() {
  const ob = PLAYER.onboarding;
  const fields = [{
    label: 'Meta',
    value: ob.goal
  }, {
    label: 'Nível',
    value: ob.fitnessLevel
  }, {
    label: 'Dias de Treino',
    value: ob.trainingDays.join(' · ')
  }, {
    label: 'Idade',
    value: `${ob.age} anos`
  }, {
    label: 'Peso',
    value: ob.weight
  }, {
    label: 'Altura',
    value: ob.height
  }];
  return /*#__PURE__*/React.createElement(Screen, null, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      marginBottom: 24
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      width: 80,
      height: 80,
      borderRadius: '50%',
      marginBottom: 14,
      background: 'linear-gradient(135deg,#2D6FF5,#8B3FD8)',
      border: '3px solid #A855F7',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      fontSize: 26,
      color: '#fff',
      boxShadow: '0 0 28px rgba(139,63,216,0.5)'
    }
  }, "VO"), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      fontSize: 20,
      color: '#F2F5FF',
      marginBottom: 8
    }
  }, PLAYER.name), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 8
    }
  }, /*#__PURE__*/React.createElement(RankBadge, {
    rank: PLAYER.rank,
    size: 30,
    glow: false
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 13,
      color: '#AEB4D0'
    }
  }, "Rank ", PLAYER.rank), /*#__PURE__*/React.createElement(Tag, {
    color: "#A855F7"
  }, PLAYER.className))), /*#__PURE__*/React.createElement(SectionLabel, null, "Atributos"), /*#__PURE__*/React.createElement(ACard, {
    style: {
      padding: '16px 18px',
      marginBottom: 20
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 14
    }
  }, Object.entries(PLAYER.stats).map(([attr, value]) => /*#__PURE__*/React.createElement(StatBar, {
    key: attr,
    attr: attr,
    value: value,
    max: 100
  })))), /*#__PURE__*/React.createElement(SectionLabel, null, "Configura\xE7\xE3o de Treino"), /*#__PURE__*/React.createElement(ACard, {
    style: {
      padding: '4px 18px',
      marginBottom: 20
    }
  }, fields.map((f, i) => /*#__PURE__*/React.createElement("div", {
    key: f.label,
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '11px 0',
      borderBottom: i < fields.length - 1 ? '1px solid rgba(255,255,255,0.05)' : 'none'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 11,
      fontWeight: 600,
      letterSpacing: '0.08em',
      textTransform: 'uppercase',
      color: '#5E6488'
    }
  }, f.label), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Sora',sans-serif",
      fontSize: 13,
      color: '#AEB4D0',
      textAlign: 'right',
      maxWidth: '58%'
    }
  }, f.value)))), /*#__PURE__*/React.createElement("button", {
    style: {
      width: '100%',
      padding: 14,
      background: 'rgba(45,111,245,0.1)',
      border: '1px solid rgba(45,111,245,0.3)',
      clipPath: CP,
      cursor: 'pointer',
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 600,
      fontSize: 13,
      letterSpacing: '0.1em',
      textTransform: 'uppercase',
      color: '#4D8BFF'
    }
  }, "Editar Perfil"));
}

// ─────────────────── INVENTÁRIO ─────────────────────────────────

function InventoryScreen({
  onItemSelect
}) {
  const [filter, setFilter] = React.useState('all');
  const chips = ['all', 'common', 'uncommon', 'rare', 'epic', 'consumable'];
  const filtered = filter === 'all' ? INVENTORY : INVENTORY.filter(i => i.rarity === filter);
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      overflow: 'hidden',
      animation: 'fadeSlideIn 240ms cubic-bezier(0.16,1,0.3,1)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      padding: '20px 20px 0',
      flexShrink: 0
    }
  }, /*#__PURE__*/React.createElement(SectionLabel, {
    right: `${INVENTORY.length} itens`
  }, "Invent\xE1rio"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 6,
      overflowX: 'auto',
      paddingBottom: 12,
      scrollbarWidth: 'none'
    }
  }, chips.map(f => {
    const rc = RARITY_CONFIG[f];
    const active = filter === f;
    const c = rc ? rc.color : '#AEB4D0';
    return /*#__PURE__*/React.createElement("button", {
      key: f,
      onClick: () => setFilter(f),
      style: {
        flexShrink: 0,
        padding: '4px 12px',
        background: active ? `color-mix(in srgb,${c} 18%,transparent)` : 'transparent',
        border: `1px solid ${active ? c : 'rgba(255,255,255,0.1)'}`,
        borderRadius: 999,
        cursor: 'pointer',
        fontFamily: "'Chakra Petch',sans-serif",
        fontSize: 10,
        fontWeight: 600,
        letterSpacing: '0.08em',
        textTransform: 'uppercase',
        color: active ? c : '#5E6488',
        transition: 'all 140ms'
      }
    }, f === 'all' ? 'Todos' : rc.label);
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      overflowY: 'auto',
      WebkitOverflowScrolling: 'touch',
      padding: '0 20px 16px'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(3,1fr)',
      gap: 8
    }
  }, filtered.map(item => {
    const rc = RARITY_CONFIG[item.rarity] || {
      color: '#333A55',
      label: '?'
    };
    return /*#__PURE__*/React.createElement("button", {
      key: item.id,
      onClick: () => onItemSelect(item),
      style: {
        background: '#111320',
        border: `1px solid color-mix(in srgb,${rc.color} 28%,rgba(255,255,255,0.05))`,
        borderRadius: 8,
        padding: '12px 8px',
        cursor: 'pointer',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 7,
        transition: 'all 140ms',
        position: 'relative'
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontSize: 26,
        lineHeight: 1
      }
    }, item.icon), /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "'Sora',sans-serif",
        fontSize: 10,
        fontWeight: 600,
        color: '#C3C9E6',
        textAlign: 'center',
        lineHeight: 1.35
      }
    }, item.name), /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "'Chakra Petch',sans-serif",
        fontSize: 9,
        fontWeight: 700,
        letterSpacing: '0.06em',
        textTransform: 'uppercase',
        color: rc.color
      }
    }, rc.label), item.qty != null && /*#__PURE__*/React.createElement("span", {
      style: {
        position: 'absolute',
        top: 5,
        right: 7,
        fontFamily: "'JetBrains Mono',monospace",
        fontSize: 10,
        color: '#5E6488'
      }
    }, "\xD7", item.qty));
  }))));
}

// ──────────────────────── LOJA ───────────────────────────────────

function ShopScreen() {
  const balances = [{
    icon: '🪙',
    label: 'OURO',
    value: PLAYER.gold.toLocaleString('pt-BR'),
    color: '#FFD64A'
  }, {
    icon: '💎',
    label: 'GEMAS',
    value: PLAYER.gems,
    color: '#5FE8FF'
  }];
  return /*#__PURE__*/React.createElement(Screen, null, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 10,
      marginBottom: 20
    }
  }, balances.map(b => /*#__PURE__*/React.createElement(ACard, {
    key: b.label,
    style: {
      flex: 1,
      padding: '12px 16px',
      display: 'flex',
      alignItems: 'center',
      gap: 10
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 22
    }
  }, b.icon), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 10,
      letterSpacing: '0.1em',
      textTransform: 'uppercase',
      color: '#5E6488'
    }
  }, b.label), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 18,
      fontWeight: 700,
      color: b.color
    }
  }, b.value))))), /*#__PURE__*/React.createElement(SectionLabel, null, "Destaque"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 10
    }
  }, SHOP_ITEMS.map(item => {
    const rc = RARITY_CONFIG[item.rarity] || {};
    const price = item.priceGold != null ? {
      icon: '🪙',
      value: item.priceGold.toLocaleString('pt-BR'),
      color: '#FFD64A'
    } : {
      icon: '💎',
      value: item.priceGems,
      color: '#5FE8FF'
    };
    return /*#__PURE__*/React.createElement(ACard, {
      key: item.id,
      style: {
        padding: '14px 16px'
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        alignItems: 'center',
        gap: 14
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        width: 48,
        height: 48,
        background: '#161929',
        border: `1px solid color-mix(in srgb,${rc.color || '#333'} 28%,transparent)`,
        borderRadius: 8,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        fontSize: 24,
        flexShrink: 0
      }
    }, item.icon), /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "'Sora',sans-serif",
        fontWeight: 600,
        fontSize: 14,
        color: '#F2F5FF',
        marginBottom: 3
      }
    }, item.name), /*#__PURE__*/React.createElement("div", {
      style: {
        fontFamily: "'Sora',sans-serif",
        fontSize: 12,
        color: '#5E6488'
      }
    }, item.desc)), /*#__PURE__*/React.createElement("button", {
      style: {
        flexShrink: 0,
        padding: '8px 12px',
        background: `color-mix(in srgb,${price.color} 14%,transparent)`,
        border: `1px solid color-mix(in srgb,${price.color} 40%,transparent)`,
        borderRadius: 6,
        cursor: 'pointer',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 2
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontSize: 14
      }
    }, price.icon), /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "'JetBrains Mono',monospace",
        fontSize: 11,
        fontWeight: 700,
        color: price.color
      }
    }, price.value))));
  })));
}

// ─────────────────── CONFIGURAÇÕES ──────────────────────────────

function SettingsScreen() {
  const [musicVol, setMusicVol] = React.useState(75);
  const [fxVol, setFxVol] = React.useState(100);
  const [lang, setLang] = React.useState('pt');
  const langs = [{
    id: 'pt',
    label: 'Português'
  }, {
    id: 'en',
    label: 'English'
  }, {
    id: 'es',
    label: 'Español'
  }];
  const support = [{
    icon: '💬',
    label: 'Fale Conosco'
  }, {
    icon: '❓',
    label: 'FAQ'
  }, {
    icon: 'ℹ️',
    label: 'Sobre o Awaken'
  }];
  const sliders = [{
    label: 'Música',
    val: musicVol,
    set: setMusicVol
  }, {
    label: 'Efeitos',
    val: fxVol,
    set: setFxVol
  }];
  return /*#__PURE__*/React.createElement(Screen, null, /*#__PURE__*/React.createElement(SectionLabel, null, "Som"), /*#__PURE__*/React.createElement(ACard, {
    style: {
      padding: '4px 18px',
      marginBottom: 20
    }
  }, sliders.map((s, i) => /*#__PURE__*/React.createElement("div", {
    key: s.label,
    style: {
      padding: '12px 0',
      borderBottom: i < sliders.length - 1 ? '1px solid rgba(255,255,255,0.05)' : 'none'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      marginBottom: 10
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 12,
      fontWeight: 600,
      letterSpacing: '0.08em',
      textTransform: 'uppercase',
      color: '#AEB4D0'
    }
  }, s.label), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 12,
      color: '#5E6488'
    }
  }, s.val, "%")), /*#__PURE__*/React.createElement("input", {
    type: "range",
    min: "0",
    max: "100",
    value: s.val,
    onChange: e => s.set(Number(e.target.value)),
    style: {
      width: '100%',
      cursor: 'pointer',
      accentColor: '#2D6FF5'
    }
  })))), /*#__PURE__*/React.createElement(SectionLabel, null, "Idioma"), /*#__PURE__*/React.createElement(ACard, {
    style: {
      padding: 6,
      marginBottom: 20
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 4
    }
  }, langs.map(l => /*#__PURE__*/React.createElement("button", {
    key: l.id,
    onClick: () => setLang(l.id),
    style: {
      flex: 1,
      padding: '10px 6px',
      background: lang === l.id ? 'rgba(45,111,245,0.18)' : 'transparent',
      border: `1px solid ${lang === l.id ? 'rgba(77,139,255,0.5)' : 'transparent'}`,
      borderRadius: 6,
      cursor: 'pointer',
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 11,
      fontWeight: 600,
      letterSpacing: '0.03em',
      color: lang === l.id ? '#4D8BFF' : '#5E6488',
      transition: 'all 140ms'
    }
  }, l.label)))), /*#__PURE__*/React.createElement(SectionLabel, null, "Suporte"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 8
    }
  }, support.map(item => /*#__PURE__*/React.createElement(ACard, {
    key: item.label,
    style: {
      padding: '14px 16px',
      cursor: 'pointer'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 18
    }
  }, item.icon), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Sora',sans-serif",
      fontSize: 14,
      color: '#AEB4D0'
    }
  }, item.label)), /*#__PURE__*/React.createElement("svg", {
    width: "16",
    height: "16",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "#5E6488",
    strokeWidth: "2.5",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M9 18l6-6-6-6"
  })))))));
}

// ─────────────────── ITEM MODAL ─────────────────────────────────

function ItemModal({
  item,
  onClose
}) {
  const rc = RARITY_CONFIG[item.rarity] || {
    color: '#828AAE',
    label: '?'
  };
  return /*#__PURE__*/React.createElement("div", {
    onClick: onClose,
    style: {
      position: 'absolute',
      inset: 0,
      background: 'rgba(7,8,13,0.84)',
      backdropFilter: 'blur(8px)',
      display: 'flex',
      alignItems: 'flex-end',
      zIndex: 100
    }
  }, /*#__PURE__*/React.createElement("div", {
    onClick: e => e.stopPropagation(),
    style: {
      width: '100%',
      background: '#111320',
      border: '1px solid rgba(255,255,255,0.1)',
      borderBottom: 'none',
      borderRadius: '16px 16px 0 0',
      padding: '20px 20px 48px',
      boxShadow: '0 -14px 40px rgba(0,0,0,0.6)',
      animation: 'slideUp 260ms cubic-bezier(0.16,1,0.3,1)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      width: 36,
      height: 3,
      borderRadius: 999,
      background: 'rgba(255,255,255,0.15)',
      margin: '0 auto 20px'
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 16,
      alignItems: 'flex-start',
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      width: 64,
      height: 64,
      background: '#161929',
      border: `1.5px solid color-mix(in srgb,${rc.color} 40%,transparent)`,
      borderRadius: 10,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontSize: 30,
      flexShrink: 0,
      boxShadow: `0 0 22px color-mix(in srgb,${rc.color} 28%,transparent)`
    }
  }, item.icon), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      fontSize: 17,
      color: '#F2F5FF',
      marginBottom: 8
    }
  }, item.name), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 6,
      flexWrap: 'wrap'
    }
  }, /*#__PURE__*/React.createElement(Tag, {
    color: rc.color
  }, rc.label), /*#__PURE__*/React.createElement(Tag, {
    color: "#828AAE"
  }, item.type)))), /*#__PURE__*/React.createElement("p", {
    style: {
      fontFamily: "'Sora',sans-serif",
      fontSize: 14,
      color: '#AEB4D0',
      lineHeight: 1.65,
      margin: '0 0 20px'
    }
  }, item.desc), item.qty != null && /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 12,
      color: '#5E6488',
      marginBottom: 16
    }
  }, "Quantidade: \xD7", item.qty), /*#__PURE__*/React.createElement("button", {
    style: {
      width: '100%',
      padding: 14,
      background: `linear-gradient(135deg,color-mix(in srgb,${rc.color} 18%,rgba(45,111,245,0.1)),color-mix(in srgb,${rc.color} 10%,rgba(139,63,216,0.08)))`,
      border: `1px solid color-mix(in srgb,${rc.color} 50%,rgba(77,139,255,0.15))`,
      clipPath: CP_SM,
      cursor: 'pointer',
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 700,
      fontSize: 13,
      letterSpacing: '0.1em',
      textTransform: 'uppercase',
      color: rc.color
    }
  }, item.rarity === 'consumable' ? 'Usar Item' : 'Equipar')));
}

// ─────────────────── BOTTOM NAV ─────────────────────────────────

const NAV_TABS = [{
  id: 'home',
  label: 'HOME',
  icon: /*#__PURE__*/React.createElement("svg", {
    width: "20",
    height: "20",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "1.8",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"
  }), /*#__PURE__*/React.createElement("polyline", {
    points: "9 22 9 12 15 12 15 22"
  }))
}, {
  id: 'profile',
  label: 'PERFIL',
  icon: /*#__PURE__*/React.createElement("svg", {
    width: "20",
    height: "20",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "1.8",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"
  }), /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "7",
    r: "4"
  }))
}, {
  id: 'inventory',
  label: 'INVENTÁRIO',
  icon: /*#__PURE__*/React.createElement("svg", {
    width: "20",
    height: "20",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "1.8",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("rect", {
    x: "2",
    y: "7",
    width: "20",
    height: "14",
    rx: "2"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M16 7V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v2"
  }))
}, {
  id: 'shop',
  label: 'LOJA',
  icon: /*#__PURE__*/React.createElement("svg", {
    width: "20",
    height: "20",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "1.8",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "3",
    y1: "6",
    x2: "21",
    y2: "6"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M16 10a4 4 0 01-8 0"
  }))
}, {
  id: 'settings',
  label: 'CONFIG',
  icon: /*#__PURE__*/React.createElement("svg", {
    width: "20",
    height: "20",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "1.8",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "12",
    r: "3"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"
  }))
}];
function BottomNav({
  tab,
  onTab
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flexShrink: 0,
      display: 'flex',
      borderTop: '1px solid rgba(255,255,255,0.07)',
      background: '#080A10'
    }
  }, NAV_TABS.map(t => {
    const active = tab === t.id;
    return /*#__PURE__*/React.createElement("button", {
      key: t.id,
      onClick: () => onTab(t.id),
      style: {
        flex: 1,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 3,
        padding: '9px 2px',
        background: 'transparent',
        border: 'none',
        cursor: 'pointer',
        color: active ? '#2D6FF5' : '#5E6488',
        transition: 'color 140ms',
        minHeight: 54,
        position: 'relative'
      }
    }, active && /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        top: 0,
        left: '50%',
        transform: 'translateX(-50%)',
        width: 24,
        height: 2,
        background: '#2D6FF5',
        borderRadius: '0 0 2px 2px',
        boxShadow: '0 0 8px #2D6FF5'
      }
    }), t.icon, /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: "'Chakra Petch',sans-serif",
        fontSize: 8,
        fontWeight: 700,
        letterSpacing: '0.07em',
        textTransform: 'uppercase',
        lineHeight: 1
      }
    }, t.label));
  }));
}

// ──────────────────────── APP ────────────────────────────────────

function App() {
  const [tab, setTab] = React.useState('home');
  const [questId, setQuestId] = React.useState(null);
  const [selectedItem, setSelectedItem] = React.useState(null);
  function goTab(t) {
    setTab(t);
    setQuestId(null);
  }
  const quest = questId ? QUESTS.find(q => q.id === questId) : null;
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      width: '100%',
      height: '100%',
      background: '#0A0B12',
      position: 'relative',
      overflow: 'hidden'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      top: -100,
      left: '50%',
      transform: 'translateX(-50%)',
      width: 340,
      height: 340,
      borderRadius: '50%',
      background: 'radial-gradient(circle,rgba(45,111,245,0.07) 0%,transparent 70%)',
      pointerEvents: 'none',
      zIndex: 0
    }
  }), quest ? /*#__PURE__*/React.createElement(QuestDetailScreen, {
    quest: quest,
    onBack: () => setQuestId(null)
  }) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
    key: tab,
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      overflow: 'hidden',
      position: 'relative',
      zIndex: 1
    }
  }, tab === 'home' && /*#__PURE__*/React.createElement(HomeScreen, {
    onQuest: setQuestId,
    onProfile: () => goTab('profile')
  }), tab === 'profile' && /*#__PURE__*/React.createElement(ProfileScreen, null), tab === 'inventory' && /*#__PURE__*/React.createElement(InventoryScreen, {
    onItemSelect: setSelectedItem
  }), tab === 'shop' && /*#__PURE__*/React.createElement(ShopScreen, null), tab === 'settings' && /*#__PURE__*/React.createElement(SettingsScreen, null)), /*#__PURE__*/React.createElement(BottomNav, {
    tab: tab,
    onTab: goTab
  })), selectedItem && /*#__PURE__*/React.createElement(ItemModal, {
    item: selectedItem,
    onClose: () => setSelectedItem(null)
  }));
}
ReactDOM.createRoot(document.getElementById('root')).render(/*#__PURE__*/React.createElement(App, null));
})(); } catch (e) { __ds_ns.__errors.push({ path: "player-screen/app.jsx", error: String((e && e.message) || e) }); }

// player-screen/ui.jsx
try { (() => {
// Awaken UI Atoms — RankBadge · XPBar · StatBar
// Exports to window.* for use in app.jsx

const ATTR_CONFIG = {
  strength: {
    label: 'Força',
    color: '#FF5A3C'
  },
  agility: {
    label: 'Agilidade',
    color: '#22D3A7'
  },
  endurance: {
    label: 'Resistência',
    color: '#2D6FF5'
  },
  vitality: {
    label: 'Vitalidade',
    color: '#F5C518'
  },
  focus: {
    label: 'Foco',
    color: '#A65CEE'
  },
  wisdom: {
    label: 'Sabedoria',
    color: '#5FE8FF'
  }
};
const RARITY_CONFIG = {
  common: {
    label: 'Comum',
    color: '#6B7280'
  },
  uncommon: {
    label: 'Incomum',
    color: '#22C55E'
  },
  rare: {
    label: 'Raro',
    color: '#3B82F6'
  },
  epic: {
    label: 'Épico',
    color: '#A855F7'
  },
  legendary: {
    label: 'Lendário',
    color: '#F5C518'
  },
  consumable: {
    label: 'Consumível',
    color: '#FF9500'
  }
};
function RankBadge({
  rank = 'E',
  size = 64,
  glow = true,
  style
}) {
  const RANKS = {
    E: {
      color: '#6B7280',
      grad: 'linear-gradient(160deg,#8A92A3,#4B5160)'
    },
    D: {
      color: '#22C55E',
      grad: 'linear-gradient(160deg,#4CE07F,#15803D)'
    },
    C: {
      color: '#3B82F6',
      grad: 'linear-gradient(160deg,#5B9BFF,#1D4ED8)'
    },
    B: {
      color: '#A855F7',
      grad: 'linear-gradient(160deg,#C07BFF,#7E22CE)'
    },
    A: {
      color: '#EAB308',
      grad: 'linear-gradient(160deg,#FACC15,#B8860B)'
    },
    S: {
      color: '#EF4444',
      grad: 'linear-gradient(160deg,#FF6B5B,#C81E2C)'
    },
    SS: {
      color: '#FF5EAD',
      grad: 'linear-gradient(135deg,#EF4444,#FF5EAD,#F5C518)'
    },
    SSS: {
      color: '#5FE8FF',
      grad: 'linear-gradient(135deg,#5FE8FF,#8B3FD8,#F5C518)'
    }
  };
  const r = RANKS[rank] || RANKS.E;
  const d = size * 0.09;
  const ins = size * 0.06;
  const corners = [{
    top: -d / 2,
    left: -d / 2
  }, {
    top: -d / 2,
    left: size - d / 2
  }, {
    top: size - d / 2,
    left: -d / 2
  }, {
    top: size - d / 2,
    left: size - d / 2
  }];
  return /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      width: size,
      height: size,
      display: 'inline-flex',
      alignItems: 'center',
      justifyContent: 'center',
      flexShrink: 0,
      filter: glow ? `drop-shadow(0 0 ${size * 0.22}px color-mix(in srgb,${r.color} 65%,transparent))` : 'none',
      ...(style || {})
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: 0,
      background: r.grad,
      transform: 'rotate(45deg)'
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: ins,
      background: '#0A0B12',
      transform: 'rotate(45deg)'
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: ins,
      background: `linear-gradient(180deg,color-mix(in srgb,${r.color} 18%,transparent),transparent 55%)`,
      transform: 'rotate(45deg)'
    }
  }), corners.map((p, i) => /*#__PURE__*/React.createElement("span", {
    key: i,
    style: {
      position: 'absolute',
      ...p,
      width: d,
      height: d,
      background: r.color,
      transform: 'rotate(45deg)',
      boxShadow: `0 0 ${d * 2}px ${r.color}`
    }
  })), /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'relative',
      fontFamily: "'Chakra Petch', sans-serif",
      fontWeight: 700,
      fontSize: size * (rank.length === 3 ? 0.28 : rank.length === 2 ? 0.36 : 0.46),
      letterSpacing: '-0.01em',
      lineHeight: 1,
      color: r.color,
      textShadow: `0 0 ${size * 0.14}px color-mix(in srgb,${r.color} 80%,transparent)`
    }
  }, rank));
}
function XPBar({
  value = 0,
  max = 100,
  level,
  height = 10,
  style
}) {
  const pct = Math.max(0, Math.min(100, value / max * 100));
  return /*#__PURE__*/React.createElement("div", {
    style: style || {}
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'baseline',
      justifyContent: 'space-between',
      marginBottom: 6
    }
  }, level != null && /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontWeight: 600,
      fontSize: 12,
      letterSpacing: '0.1em',
      textTransform: 'uppercase',
      color: '#FFD64A'
    }
  }, "N\xEDvel ", level), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 11,
      color: '#828AAE'
    }
  }, Math.round(value), " / ", max, " XP")), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      height,
      borderRadius: 999,
      background: '#1D2133',
      overflow: 'hidden',
      boxShadow: 'inset 0 1px 2px rgba(0,0,0,0.5)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: 0,
      width: `${pct}%`,
      borderRadius: 999,
      background: 'linear-gradient(90deg,#F5C518,#FF9500)',
      boxShadow: '0 0 14px rgba(245,197,24,0.55)',
      transition: 'width 600ms cubic-bezier(0.16,1,0.3,1)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      inset: 0,
      background: 'linear-gradient(180deg,rgba(255,255,255,0.45),transparent 60%)',
      borderRadius: 'inherit'
    }
  }))));
}
function StatBar({
  attr = 'strength',
  value = 0,
  max = 100,
  style
}) {
  const a = ATTR_CONFIG[attr] || ATTR_CONFIG.strength;
  const pct = Math.max(0, Math.min(100, value / max * 100));
  return /*#__PURE__*/React.createElement("div", {
    style: style || {}
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'baseline',
      justifyContent: 'space-between',
      marginBottom: 5
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'Chakra Petch',sans-serif",
      fontSize: 11,
      fontWeight: 600,
      letterSpacing: '0.08em',
      textTransform: 'uppercase',
      color: '#AEB4D0'
    }
  }, a.label), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "'JetBrains Mono',monospace",
      fontSize: 13,
      fontWeight: 700,
      color: a.color
    }
  }, Math.round(value))), /*#__PURE__*/React.createElement("div", {
    style: {
      height: 7,
      borderRadius: 999,
      background: '#1D2133',
      overflow: 'hidden'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      height: '100%',
      width: `${pct}%`,
      borderRadius: 999,
      background: `linear-gradient(90deg,color-mix(in srgb,${a.color} 70%,#000),${a.color})`,
      boxShadow: `0 0 8px color-mix(in srgb,${a.color} 55%,transparent)`,
      transition: 'width 360ms cubic-bezier(0.16,1,0.3,1)'
    }
  })));
}
Object.assign(window, {
  RankBadge,
  XPBar,
  StatBar,
  ATTR_CONFIG,
  RARITY_CONFIG
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "player-screen/ui.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/app.jsx
try { (() => {
/* Awaken UI kit — app root: full flow Splash → Plans → Onboarding → main tabs. */
const {
  Splash,
  Plans,
  Onboarding
} = window;
const {
  Home,
  Profile,
  Workout,
  LevelUp,
  todayQuests
} = window;
const {
  PhoneFrame,
  TabBar
} = window;
function AwakenApp() {
  const [screen, setScreen] = React.useState('splash'); // splash|plans|onboarding|app
  const [tab, setTab] = React.useState('home'); // home|profile
  const [view, setView] = React.useState('home'); // home|profile|workout
  const [quests, setQuests] = React.useState(() => todayQuests().map(q => ({
    ...q,
    done: false
  })));
  const [levelUp, setLevelUp] = React.useState(false);
  const toggleQuest = id => setQuests(qs => qs.map(q => q.id === id ? {
    ...q,
    done: !q.done
  } : q));
  const go = v => {
    setView(v);
    if (v === 'home' || v === 'profile') setTab(v);
  };
  const changeTab = t => {
    setTab(t);
    setView(t);
  };
  const completeWorkout = () => {
    setQuests(qs => qs.map(q => ({
      ...q,
      done: true
    })));
    setLevelUp(true);
  };
  let body;
  if (screen === 'splash') body = /*#__PURE__*/React.createElement(Splash, {
    onStart: () => setScreen('plans')
  });else if (screen === 'plans') body = /*#__PURE__*/React.createElement(Plans, {
    onContinue: () => setScreen('onboarding')
  });else if (screen === 'onboarding') body = /*#__PURE__*/React.createElement(Onboarding, {
    onDone: () => {
      setScreen('app');
      go('home');
    }
  });else {
    const screenEl = view === 'profile' ? /*#__PURE__*/React.createElement(Profile, null) : view === 'workout' ? /*#__PURE__*/React.createElement(Workout, {
      go: go,
      quests: quests,
      completeWorkout: completeWorkout
    }) : /*#__PURE__*/React.createElement(Home, {
      go: go,
      quests: quests,
      toggleQuest: toggleQuest
    });
    body = /*#__PURE__*/React.createElement(React.Fragment, null, screenEl, view !== 'workout' && /*#__PURE__*/React.createElement(TabBar, {
      active: tab,
      onChange: changeTab,
      onTrain: () => go('workout')
    }), levelUp && /*#__PURE__*/React.createElement(LevelUp, {
      onClose: () => {
        setLevelUp(false);
        go('home');
      }
    }));
  }
  return /*#__PURE__*/React.createElement(PhoneFrame, null, body);
}
ReactDOM.createRoot(document.getElementById('root')).render(/*#__PURE__*/React.createElement(AwakenApp, null));
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/app.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/chrome.jsx
try { (() => {
/* Awaken UI kit — shared chrome: phone frame, status bar, tab bar, screen header. */
const Icon = window.AwakenIcon;
function StatusBar({
  dark = false
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      height: 44,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '0 24px',
      flexShrink: 0,
      color: 'var(--text-primary)',
      fontFamily: 'var(--font-mono)',
      fontSize: 13,
      fontWeight: 500
    }
  }, /*#__PURE__*/React.createElement("span", {
    className: "tnum"
  }, "9:41"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 6
    }
  }, /*#__PURE__*/React.createElement("svg", {
    width: "18",
    height: "12",
    viewBox: "0 0 18 12",
    fill: "currentColor"
  }, /*#__PURE__*/React.createElement("rect", {
    x: "0",
    y: "7",
    width: "3",
    height: "5",
    rx: "1"
  }), /*#__PURE__*/React.createElement("rect", {
    x: "5",
    y: "4",
    width: "3",
    height: "8",
    rx: "1"
  }), /*#__PURE__*/React.createElement("rect", {
    x: "10",
    y: "1.5",
    width: "3",
    height: "10.5",
    rx: "1",
    opacity: "0.5"
  }), /*#__PURE__*/React.createElement("rect", {
    x: "15",
    y: "0",
    width: "3",
    height: "12",
    rx: "1",
    opacity: "0.3"
  })), /*#__PURE__*/React.createElement("svg", {
    width: "16",
    height: "12",
    viewBox: "0 0 16 12",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "1.4"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M1 4.5C4.5 1.5 11.5 1.5 15 4.5M3 7C5.5 5 10.5 5 13 7M5.5 9.3C7 8.2 9 8.2 10.5 9.3"
  })), /*#__PURE__*/React.createElement("svg", {
    width: "24",
    height: "12",
    viewBox: "0 0 24 12",
    fill: "none"
  }, /*#__PURE__*/React.createElement("rect", {
    x: "1",
    y: "1",
    width: "20",
    height: "10",
    rx: "2.5",
    stroke: "currentColor",
    strokeOpacity: "0.4"
  }), /*#__PURE__*/React.createElement("rect", {
    x: "3",
    y: "3",
    width: "15",
    height: "6",
    rx: "1",
    fill: "currentColor"
  }), /*#__PURE__*/React.createElement("rect", {
    x: "22",
    y: "4",
    width: "1.5",
    height: "4",
    rx: "0.75",
    fill: "currentColor",
    fillOpacity: "0.4"
  }))));
}
function PhoneFrame({
  children,
  glow = true
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      width: 390,
      height: 'min(844px, 92vh)',
      maxHeight: 844,
      borderRadius: 46,
      padding: 5,
      background: 'linear-gradient(160deg, #23263a, #0d0f18)',
      boxShadow: glow ? '0 40px 100px rgba(0,0,0,0.6), 0 0 0 1px rgba(255,255,255,0.05), 0 0 90px rgba(45,111,245,0.18)' : '0 40px 100px rgba(0,0,0,0.6)',
      flexShrink: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      width: '100%',
      height: '100%',
      borderRadius: 42,
      overflow: 'hidden',
      position: 'relative',
      background: 'var(--bg-base)',
      display: 'flex',
      flexDirection: 'column'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      top: 9,
      left: '50%',
      transform: 'translateX(-50%)',
      width: 116,
      height: 30,
      background: '#000',
      borderRadius: 999,
      zIndex: 50
    }
  }), children));
}
function ScreenScroll({
  children,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      overflowY: 'auto',
      overflowX: 'hidden',
      WebkitOverflowScrolling: 'touch',
      ...style
    }
  }, children);
}
function AppHeader({
  title,
  eyebrow,
  left,
  right
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      gap: 12,
      padding: '6px 20px 14px'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 12,
      minWidth: 0
    }
  }, left, /*#__PURE__*/React.createElement("div", {
    style: {
      minWidth: 0
    }
  }, eyebrow && /*#__PURE__*/React.createElement("div", {
    className: "eyebrow",
    style: {
      marginBottom: 2
    }
  }, eyebrow), /*#__PURE__*/React.createElement("h1", {
    style: {
      fontSize: 22,
      fontWeight: 700,
      color: 'var(--text-primary)',
      whiteSpace: 'nowrap',
      overflow: 'hidden',
      textOverflow: 'ellipsis'
    }
  }, title))), right);
}
function IconBtn({
  name,
  onClick,
  badge
}) {
  return /*#__PURE__*/React.createElement("button", {
    onClick: onClick,
    style: {
      position: 'relative',
      width: 40,
      height: 40,
      borderRadius: 'var(--radius-md)',
      display: 'grid',
      placeItems: 'center',
      background: 'var(--bg-surface)',
      border: '1px solid var(--border-default)',
      color: 'var(--text-secondary)',
      cursor: 'pointer',
      flexShrink: 0
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: name,
    size: 19
  }), badge && /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      top: 8,
      right: 8,
      width: 7,
      height: 7,
      borderRadius: '50%',
      background: 'var(--danger)',
      boxShadow: '0 0 6px var(--danger)'
    }
  }));
}
function TabBar({
  active,
  onChange,
  onTrain
}) {
  const Tab = ({
    id,
    icon,
    label
  }) => {
    const on = active === id;
    return /*#__PURE__*/React.createElement("button", {
      onClick: () => onChange(id),
      style: {
        flex: 1,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 4,
        padding: '8px 0',
        background: 'none',
        border: 'none',
        cursor: 'pointer',
        color: on ? 'var(--blue-300)' : 'var(--text-tertiary)'
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: icon,
      size: 22,
      strokeWidth: on ? 2.4 : 2
    }), /*#__PURE__*/React.createElement("span", {
      style: {
        fontFamily: 'var(--font-display)',
        fontSize: 10,
        fontWeight: 600,
        letterSpacing: '0.06em',
        textTransform: 'uppercase'
      }
    }, label));
  };
  return /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      flexShrink: 0,
      display: 'flex',
      alignItems: 'flex-end',
      padding: '0 14px 22px',
      background: 'linear-gradient(0deg, var(--bg-base) 60%, transparent)',
      borderTop: '1px solid var(--border-subtle)'
    }
  }, /*#__PURE__*/React.createElement(Tab, {
    id: "home",
    icon: "home",
    label: "In\xEDcio"
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      width: 76,
      display: 'flex',
      justifyContent: 'center',
      position: 'relative'
    }
  }, /*#__PURE__*/React.createElement("button", {
    onClick: onTrain,
    style: {
      position: 'absolute',
      bottom: 6,
      width: 62,
      height: 62,
      borderRadius: '50%',
      display: 'grid',
      placeItems: 'center',
      background: 'var(--grad-energy)',
      border: '3px solid var(--bg-base)',
      color: '#fff',
      cursor: 'pointer',
      boxShadow: 'var(--glow-blue)'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "swords",
    size: 26,
    strokeWidth: 2.2
  }))), /*#__PURE__*/React.createElement(Tab, {
    id: "profile",
    icon: "user",
    label: "Perfil"
  }));
}
Object.assign(window, {
  StatusBar,
  PhoneFrame,
  ScreenScroll,
  AppHeader,
  IconBtn,
  TabBar
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/chrome.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/icons.jsx
try { (() => {
/* Awaken UI kit — inline Lucide line icons (uniform 2px stroke), copied in to avoid a
 * CDN dependency. <Icon name="dumbbell" size={20} />. Inherits currentColor. */
const ICONS = {
  dumbbell: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M14.4 14.4 9.6 9.6"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M18.657 21.485a2 2 0 1 1-2.829-2.828l-1.767 1.768a2 2 0 1 1-2.829-2.829l6.364-6.364a2 2 0 1 1 2.829 2.829l-1.768 1.767a2 2 0 1 1 2.828 2.829z"
  }), /*#__PURE__*/React.createElement("path", {
    d: "m21.5 21.5-1.4-1.4"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M3.9 3.9 2.5 2.5"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M6.404 12.768a2 2 0 1 1-2.829-2.829l1.768-1.767a2 2 0 1 1-2.828-2.829l2.828-2.828a2 2 0 1 1 2.829 2.828l1.767-1.768a2 2 0 1 1 2.829 2.829z"
  })),
  flame: /*#__PURE__*/React.createElement("path", {
    d: "M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z"
  }),
  zap: /*#__PURE__*/React.createElement("path", {
    d: "M4 14a1 1 0 0 1-.78-1.63l9.9-10.2a.5.5 0 0 1 .86.46l-1.92 6.02A1 1 0 0 0 13 10h7a1 1 0 0 1 .78 1.63l-9.9 10.2a.5.5 0 0 1-.86-.46l1.92-6.02A1 1 0 0 0 11 14z"
  }),
  target: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "12",
    r: "10"
  }), /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "12",
    r: "6"
  }), /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "12",
    r: "2"
  })),
  activity: /*#__PURE__*/React.createElement("path", {
    d: "M22 12h-2.48a2 2 0 0 0-1.93 1.46l-2.35 8.36a.25.25 0 0 1-.48 0L9.24 2.18a.25.25 0 0 0-.48 0l-2.35 8.36A2 2 0 0 1 4.49 12H2"
  }),
  shield: /*#__PURE__*/React.createElement("path", {
    d: "M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z"
  }),
  droplet: /*#__PURE__*/React.createElement("path", {
    d: "M12 22a7 7 0 0 0 7-7c0-2-1-3.9-3-5.5s-3.5-4-4-6.5c-.5 2.5-2 4.9-4 6.5C6 11.1 5 13 5 15a7 7 0 0 0 7 7z"
  }),
  trophy: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M6 9H4.5a2.5 2.5 0 0 1 0-5H6"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M18 9h1.5a2.5 2.5 0 0 0 0-5H18"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M4 22h16"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M18 2H6v7a6 6 0 0 0 12 0V2Z"
  })),
  user: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"
  }), /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "7",
    r: "4"
  })),
  home: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M15 21v-8a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v8"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M3 10a2 2 0 0 1 .709-1.528l7-5.999a2 2 0 0 1 2.582 0l7 5.999A2 2 0 0 1 21 10v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"
  })),
  swords: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("polyline", {
    points: "14.5 17.5 3 6 3 3 6 3 17.5 14.5"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "13",
    x2: "19",
    y1: "19",
    y2: "13"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "16",
    x2: "20",
    y1: "16",
    y2: "20"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "19",
    x2: "21",
    y1: "21",
    y2: "19"
  }), /*#__PURE__*/React.createElement("polyline", {
    points: "14.5 6.5 18 3 21 3 21 6 17.5 9.5"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "5",
    x2: "9",
    y1: "14",
    y2: "18"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "7",
    x2: "4",
    y1: "17",
    y2: "20"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "3",
    x2: "5",
    y1: "19",
    y2: "21"
  })),
  chevronRight: /*#__PURE__*/React.createElement("path", {
    d: "m9 18 6-6-6-6"
  }),
  chevronLeft: /*#__PURE__*/React.createElement("path", {
    d: "m15 18-6-6 6-6"
  }),
  chevronDown: /*#__PURE__*/React.createElement("path", {
    d: "m6 9 6 6 6-6"
  }),
  check: /*#__PURE__*/React.createElement("path", {
    d: "M20 6 9 17l-5-5"
  }),
  x: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M18 6 6 18"
  }), /*#__PURE__*/React.createElement("path", {
    d: "m6 6 12 12"
  })),
  plus: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M5 12h14"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M12 5v14"
  })),
  lock: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("rect", {
    width: "18",
    height: "11",
    x: "3",
    y: "11",
    rx: "2",
    ry: "2"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M7 11V7a5 5 0 0 1 10 0v4"
  })),
  play: /*#__PURE__*/React.createElement("polygon", {
    points: "6 3 20 12 6 21 6 3"
  }),
  pause: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("rect", {
    x: "14",
    y: "4",
    width: "4",
    height: "16",
    rx: "1"
  }), /*#__PURE__*/React.createElement("rect", {
    x: "6",
    y: "4",
    width: "4",
    height: "16",
    rx: "1"
  })),
  bell: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M10.268 21a2 2 0 0 0 3.464 0"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M3.262 15.326A1 1 0 0 0 4 17h16a1 1 0 0 0 .74-1.673C19.41 13.956 18 12.499 18 8A6 6 0 0 0 6 8c0 4.499-1.411 5.956-2.738 7.326"
  })),
  settings: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"
  }), /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "12",
    r: "3"
  })),
  crown: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M11.562 3.266a.5.5 0 0 1 .876 0L15.39 8.87a1 1 0 0 0 1.516.294L21.183 5.5a.5.5 0 0 1 .798.519l-2.834 10.246a1 1 0 0 1-.956.734H5.81a1 1 0 0 1-.957-.734L2.02 6.02a.5.5 0 0 1 .798-.519l4.276 3.664a1 1 0 0 0 1.516-.294z"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M5 21h14"
  })),
  wind: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M12.8 19.6A2 2 0 1 0 14 16H2"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M17.5 8a2.5 2.5 0 1 1 2 4H2"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M9.8 4.4A2 2 0 1 1 11 8H2"
  })),
  calendar: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M8 2v4"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M16 2v4"
  }), /*#__PURE__*/React.createElement("rect", {
    width: "18",
    height: "18",
    x: "3",
    y: "4",
    rx: "2"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M3 10h18"
  })),
  arrowRight: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M5 12h14"
  }), /*#__PURE__*/React.createElement("path", {
    d: "m12 5 7 7-7 7"
  })),
  timer: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("line", {
    x1: "10",
    x2: "14",
    y1: "2",
    y2: "2"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "12",
    x2: "15",
    y1: "14",
    y2: "11"
  }), /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "14",
    r: "8"
  })),
  share: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8"
  }), /*#__PURE__*/React.createElement("polyline", {
    points: "16 6 12 2 8 6"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "12",
    x2: "12",
    y1: "2",
    y2: "15"
  })),
  eye: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
    d: "M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7z"
  }), /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "12",
    r: "3"
  }))
};
function Icon({
  name,
  size = 20,
  color = 'currentColor',
  strokeWidth = 2,
  style
}) {
  return /*#__PURE__*/React.createElement("svg", {
    width: size,
    height: size,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: color,
    strokeWidth: strokeWidth,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    style: {
      display: 'block',
      flexShrink: 0,
      ...style
    }
  }, ICONS[name] || null);
}
window.AwakenIcon = Icon;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/icons.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/screens-main.jsx
try { (() => {
/* Awaken UI kit — main app: Home (daily quests), Profile (hunter card), Workout. */
const Icon = window.AwakenIcon;
const {
  Button,
  Badge,
  Card,
  Avatar,
  RankBadge,
  XPBar,
  StatBar,
  QuestCard,
  ProgressRing
} = window.AwakenDesignSystem_956798;
const {
  StatusBar,
  ScreenScroll,
  AppHeader,
  IconBtn,
  TabBar
} = window;
const HUNTER = {
  name: 'Kael Voss',
  rank: 'B',
  level: 37,
  klass: 'Striker',
  streak: 12,
  xp: 640,
  xpMax: 900,
  attrs: {
    strength: 72,
    agility: 54,
    endurance: 61,
    vitality: 68,
    focus: 45,
    wisdom: 58
  }
};
const QUEST_ICON = {
  strength: 'dumbbell',
  agility: 'wind',
  endurance: 'activity',
  vitality: 'shield',
  focus: 'target',
  wisdom: 'eye'
};
function todayQuests() {
  return [{
    id: 1,
    title: 'Flexões — 4 × 12',
    subtitle: 'Peito · sem equipamento',
    xp: 120,
    attr: 'strength'
  }, {
    id: 2,
    title: 'Agachamento livre — 4 × 15',
    subtitle: 'Pernas · sem equipamento',
    xp: 140,
    attr: 'endurance'
  }, {
    id: 3,
    title: 'Prancha — 3 × 45s',
    subtitle: 'Core · foco',
    xp: 80,
    attr: 'focus'
  }, {
    id: 4,
    title: 'Burpees — 3 × 10',
    subtitle: 'Full body · cardio',
    xp: 110,
    attr: 'agility'
  }];
}

/* ---------------- Home ---------------- */
function Home({
  go,
  quests,
  toggleQuest
}) {
  const doneCount = quests.filter(q => q.done).length;
  const earned = quests.filter(q => q.done).reduce((s, q) => s + q.xp, 0);
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      background: 'var(--grad-void)'
    }
  }, /*#__PURE__*/React.createElement(StatusBar, null), /*#__PURE__*/React.createElement(AppHeader, {
    eyebrow: "Bom treino, hunter",
    title: "Kael",
    left: /*#__PURE__*/React.createElement(Avatar, {
      name: HUNTER.name,
      rank: HUNTER.rank,
      size: 44,
      onClick: () => go('profile'),
      style: {
        cursor: 'pointer'
      }
    }),
    right: /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        gap: 8
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        alignItems: 'center',
        gap: 5,
        padding: '0 11px',
        height: 40,
        borderRadius: 'var(--radius-md)',
        background: 'var(--bg-surface)',
        border: '1px solid var(--border-default)'
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "flame",
      size: 17,
      color: "var(--orange-500)"
    }), /*#__PURE__*/React.createElement("span", {
      className: "tnum",
      style: {
        fontFamily: 'var(--font-display)',
        fontWeight: 700,
        fontSize: 15,
        color: 'var(--text-primary)'
      }
    }, HUNTER.streak)), /*#__PURE__*/React.createElement(IconBtn, {
      name: "bell",
      badge: true
    }))
  }), /*#__PURE__*/React.createElement(ScreenScroll, {
    style: {
      padding: '0 20px 24px'
    }
  }, /*#__PURE__*/React.createElement(Card, {
    variant: "energy",
    padding: 18,
    style: {
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 16,
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement(RankBadge, {
    rank: HUNTER.rank,
    size: 66
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 8
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 19,
      color: 'var(--text-primary)'
    }
  }, "Rank ", HUNTER.rank), /*#__PURE__*/React.createElement(Badge, {
    tone: "purple",
    variant: "soft"
  }, HUNTER.klass)), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 13,
      color: 'var(--text-tertiary)',
      marginTop: 3
    }
  }, "Faltam ", HUNTER.xpMax - HUNTER.xp, " XP para o pr\xF3ximo n\xEDvel"))), /*#__PURE__*/React.createElement(XPBar, {
    level: HUNTER.level,
    value: HUNTER.xp,
    max: HUNTER.xpMax
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      margin: '4px 2px 12px'
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "eyebrow",
    style: {
      color: 'var(--text-secondary)'
    }
  }, "Quest di\xE1ria \xB7 hoje"), /*#__PURE__*/React.createElement("span", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 12,
      color: 'var(--text-tertiary)'
    }
  }, doneCount, "/", quests.length, " \xB7 +", earned, " XP")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 10,
      marginBottom: 20
    }
  }, quests.map((q, i) => /*#__PURE__*/React.createElement(QuestCard, {
    key: q.id,
    title: q.title,
    subtitle: q.subtitle,
    xp: q.xp,
    attr: q.attr,
    status: q.done ? 'done' : i === doneCount ? 'active' : 'todo',
    icon: /*#__PURE__*/React.createElement(Icon, {
      name: QUEST_ICON[q.attr],
      size: 20
    }),
    onToggle: () => toggleQuest(q.id),
    onClick: () => go('workout')
  }))), /*#__PURE__*/React.createElement(Button, {
    variant: "primary",
    size: "lg",
    glow: true,
    fullWidth: true,
    onClick: () => go('workout'),
    leftIcon: /*#__PURE__*/React.createElement(Icon, {
      name: "play",
      size: 17
    })
  }, "Iniciar treino completo"), /*#__PURE__*/React.createElement("div", {
    className: "eyebrow",
    style: {
      color: 'var(--text-secondary)',
      margin: '24px 2px 12px'
    }
  }, "Status do dia"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 12
    }
  }, [{
    ring: /*#__PURE__*/React.createElement(ProgressRing, {
      value: 6,
      max: 8,
      size: 84,
      color: "var(--info)",
      label: "6",
      sublabel: "/ 8 copos"
    }),
    icon: 'droplet',
    label: 'Água'
  }, {
    ring: /*#__PURE__*/React.createElement(ProgressRing, {
      value: 88,
      max: 140,
      size: 84,
      color: "var(--attr-strength)",
      label: "88",
      sublabel: "/ 140 g"
    }),
    icon: 'zap',
    label: 'Proteína'
  }, {
    ring: /*#__PURE__*/React.createElement(ProgressRing, {
      value: 1820,
      max: 2200,
      size: 84,
      color: "var(--attr-vitality)",
      label: "1.8k",
      sublabel: "kcal"
    }),
    icon: 'flame',
    label: 'Calorias'
  }].map(t => /*#__PURE__*/React.createElement(Card, {
    key: t.label,
    padding: 12,
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      gap: 8
    }
  }, t.ring, /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 12,
      color: 'var(--text-tertiary)'
    }
  }, t.label))))));
}

/* ---------------- Profile (Hunter card) ---------------- */
const ATTR_META = [{
  key: 'strength',
  label: 'Força',
  icon: 'dumbbell'
}, {
  key: 'agility',
  label: 'Agilidade',
  icon: 'wind'
}, {
  key: 'endurance',
  label: 'Resistência',
  icon: 'activity'
}, {
  key: 'vitality',
  label: 'Vitalidade',
  icon: 'shield'
}, {
  key: 'focus',
  label: 'Foco',
  icon: 'target'
}, {
  key: 'wisdom',
  label: 'Sabedoria',
  icon: 'eye'
}];
function Profile() {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      background: 'var(--grad-void)'
    }
  }, /*#__PURE__*/React.createElement(StatusBar, null), /*#__PURE__*/React.createElement(AppHeader, {
    eyebrow: "Hunter",
    title: "Perfil",
    right: /*#__PURE__*/React.createElement(IconBtn, {
      name: "settings"
    })
  }), /*#__PURE__*/React.createElement(ScreenScroll, {
    style: {
      padding: '0 20px 24px'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      borderRadius: 22,
      overflow: 'hidden',
      padding: 22,
      background: 'radial-gradient(130% 100% at 50% 0%, #1a2348, #0b0e1c 70%)',
      border: '1px solid color-mix(in srgb, var(--rank-b) 45%, transparent)',
      boxShadow: '0 0 34px color-mix(in srgb, var(--rank-b) 26%, transparent)',
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      top: -30,
      right: -30,
      width: 160,
      height: 160,
      background: 'radial-gradient(circle, rgba(139,63,216,0.4), transparent 65%)'
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "eyebrow",
    style: {
      color: 'var(--blue-200)'
    }
  }, "Hunter Card"), /*#__PURE__*/React.createElement(IconBtn, {
    name: "share"
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      textAlign: 'center'
    }
  }, /*#__PURE__*/React.createElement(Avatar, {
    name: HUNTER.name,
    rank: HUNTER.rank,
    size: 92
  }), /*#__PURE__*/React.createElement("h2", {
    style: {
      fontSize: 24,
      fontWeight: 700,
      color: '#fff',
      marginTop: 14
    }
  }, HUNTER.name), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 8,
      marginTop: 8
    }
  }, /*#__PURE__*/React.createElement(RankBadge, {
    rank: HUNTER.rank,
    size: 30,
    glow: false
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 600,
      fontSize: 14,
      color: 'var(--text-secondary)',
      letterSpacing: '0.05em'
    }
  }, "RANK ", HUNTER.rank, " \xB7 N\xCDVEL ", HUNTER.level)), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 8,
      marginTop: 14
    }
  }, /*#__PURE__*/React.createElement(Badge, {
    tone: "purple",
    variant: "soft"
  }, HUNTER.klass), /*#__PURE__*/React.createElement(Badge, {
    tone: "red",
    variant: "soft",
    icon: /*#__PURE__*/React.createElement(Icon, {
      name: "flame",
      size: 12
    })
  }, "Streak ", HUNTER.streak)))), /*#__PURE__*/React.createElement("div", {
    className: "eyebrow",
    style: {
      color: 'var(--text-secondary)',
      margin: '6px 2px 14px'
    }
  }, "Atributos"), /*#__PURE__*/React.createElement(Card, {
    padding: 18,
    style: {
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 15
    }
  }, ATTR_META.map(a => /*#__PURE__*/React.createElement(StatBar, {
    key: a.key,
    attr: a.key,
    value: HUNTER.attrs[a.key]
  })))), /*#__PURE__*/React.createElement("div", {
    className: "eyebrow",
    style: {
      color: 'var(--text-secondary)',
      margin: '6px 2px 14px'
    }
  }, "Conquistas"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 10
    }
  }, [{
    icon: 'flame',
    tone: 'var(--orange-500)',
    label: 'Streak 7'
  }, {
    icon: 'trophy',
    tone: 'var(--gold-400)',
    label: 'Rank up'
  }, {
    icon: 'zap',
    tone: 'var(--blue-300)',
    label: '10k XP'
  }, {
    icon: 'lock',
    tone: 'var(--text-disabled)',
    label: 'Master',
    locked: true
  }].map(b => /*#__PURE__*/React.createElement(Card, {
    key: b.label,
    padding: 12,
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      gap: 9,
      opacity: b.locked ? 0.5 : 1
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      width: 46,
      height: 46,
      borderRadius: '50%',
      display: 'grid',
      placeItems: 'center',
      background: 'var(--bg-elevated)',
      border: `1px solid ${b.locked ? 'var(--border-default)' : 'color-mix(in srgb,' + b.tone + ' 45%, transparent)'}`,
      color: b.tone,
      boxShadow: b.locked ? 'none' : `0 0 14px color-mix(in srgb, ${b.tone} 30%, transparent)`
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: b.icon,
    size: 20
  })), /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 11,
      color: 'var(--text-tertiary)'
    }
  }, b.label))))));
}

/* ---------------- Workout (live quest) ---------------- */
function Workout({
  go,
  quests,
  completeWorkout
}) {
  const [set, setSet] = React.useState(1);
  const totalSets = 4;
  const ex = {
    name: 'Flexões',
    target: '12 repetições',
    attr: 'strength',
    xp: 120,
    muscle: 'Peito · Tríceps'
  };
  const nextSet = () => {
    if (set < totalSets) setSet(set + 1);else completeWorkout();
  };
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      background: 'var(--grad-void)'
    }
  }, /*#__PURE__*/React.createElement(StatusBar, null), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '4px 20px 10px'
    }
  }, /*#__PURE__*/React.createElement("button", {
    onClick: () => go('home'),
    style: {
      width: 40,
      height: 40,
      borderRadius: 'var(--radius-md)',
      background: 'var(--bg-surface)',
      border: '1px solid var(--border-default)',
      display: 'grid',
      placeItems: 'center',
      color: 'var(--text-secondary)',
      cursor: 'pointer'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "chevronLeft",
    size: 18
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 6,
      fontFamily: 'var(--font-mono)',
      fontSize: 14,
      color: 'var(--text-secondary)'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "timer",
    size: 16,
    color: "var(--blue-300)"
  }), /*#__PURE__*/React.createElement("span", {
    className: "tnum"
  }, "12:48")), /*#__PURE__*/React.createElement("button", {
    onClick: () => go('home'),
    style: {
      width: 40,
      height: 40,
      borderRadius: 'var(--radius-md)',
      background: 'var(--bg-surface)',
      border: '1px solid var(--border-default)',
      display: 'grid',
      placeItems: 'center',
      color: 'var(--text-secondary)',
      cursor: 'pointer'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "x",
    size: 18
  }))), /*#__PURE__*/React.createElement(ScreenScroll, {
    style: {
      padding: '8px 22px 0',
      display: 'flex',
      flexDirection: 'column'
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "eyebrow",
    style: {
      color: 'var(--blue-300)'
    }
  }, "Quest em andamento \xB7 1 de ", quests.length), /*#__PURE__*/React.createElement("h1", {
    style: {
      fontSize: 34,
      fontWeight: 700,
      color: 'var(--text-primary)',
      margin: '6px 0 4px',
      textTransform: 'uppercase'
    }
  }, ex.name), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 8,
      marginBottom: 22
    }
  }, /*#__PURE__*/React.createElement(Badge, {
    tone: "red",
    variant: "soft"
  }, "For\xE7a"), /*#__PURE__*/React.createElement(Badge, {
    tone: "neutral",
    variant: "outline"
  }, ex.muscle)), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'center',
      margin: '8px 0 24px'
    }
  }, /*#__PURE__*/React.createElement(ProgressRing, {
    value: set,
    max: totalSets,
    size: 200,
    stroke: 14,
    color: "energy"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      textAlign: 'center'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 11,
      letterSpacing: '0.14em',
      textTransform: 'uppercase',
      color: 'var(--text-tertiary)'
    }
  }, "S\xE9rie"), /*#__PURE__*/React.createElement("div", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 56,
      color: 'var(--text-primary)',
      lineHeight: 1
    }
  }, set, /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 26,
      color: 'var(--text-tertiary)'
    }
  }, "/", totalSets)), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 16,
      fontWeight: 600,
      color: 'var(--blue-200)',
      marginTop: 4,
      textTransform: 'uppercase'
    }
  }, ex.target)))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 8,
      justifyContent: 'center',
      marginBottom: 26
    }
  }, Array.from({
    length: totalSets
  }).map((_, i) => /*#__PURE__*/React.createElement("div", {
    key: i,
    style: {
      width: 44,
      height: 6,
      borderRadius: 999,
      background: i < set ? 'var(--grad-energy)' : 'var(--ink-700)',
      boxShadow: i < set ? 'var(--glow-blue-sm)' : 'none'
    }
  }))), /*#__PURE__*/React.createElement(Card, {
    padding: 14,
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "wind",
    size: 18,
    color: "var(--text-tertiary)"
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 14,
      color: 'var(--text-secondary)'
    }
  }, "Muito dif\xEDcil? Trocar variante")), /*#__PURE__*/React.createElement(Icon, {
    name: "chevronRight",
    size: 18,
    color: "var(--text-tertiary)"
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      padding: '8px 22px 34px'
    }
  }, /*#__PURE__*/React.createElement(Button, {
    variant: "primary",
    size: "lg",
    glow: true,
    fullWidth: true,
    onClick: nextSet,
    rightIcon: /*#__PURE__*/React.createElement(Icon, {
      name: set < totalSets ? 'check' : 'zap',
      size: 18
    })
  }, set < totalSets ? 'Série concluída' : `Finalizar · +${ex.xp} XP`)));
}

/* ---------------- Level-up overlay ---------------- */
function LevelUp({
  onClose
}) {
  return /*#__PURE__*/React.createElement("div", {
    onClick: onClose,
    style: {
      position: 'absolute',
      inset: 0,
      zIndex: 60,
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'radial-gradient(circle at 50% 40%, rgba(45,111,245,0.35), rgba(7,8,13,0.92) 60%)',
      backdropFilter: 'blur(4px)',
      padding: 32,
      textAlign: 'center',
      cursor: 'pointer'
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "eyebrow",
    style: {
      color: 'var(--gold-400)',
      fontSize: 13
    }
  }, "Quest completa"), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      margin: '18px 0'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: -30,
      background: 'radial-gradient(circle, rgba(139,63,216,0.5), transparent 65%)'
    }
  }), /*#__PURE__*/React.createElement(RankBadge, {
    rank: "B",
    size: 130
  })), /*#__PURE__*/React.createElement("h1", {
    style: {
      fontSize: 30,
      fontWeight: 700,
      color: '#fff',
      textTransform: 'uppercase',
      lineHeight: 1.1
    }
  }, "Voc\xEA subiu para", /*#__PURE__*/React.createElement("br", null), "o n\xEDvel 38"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 10,
      marginTop: 18
    }
  }, /*#__PURE__*/React.createElement(Badge, {
    tone: "gold",
    variant: "solid"
  }, "+450 XP"), /*#__PURE__*/React.createElement(Badge, {
    tone: "red",
    variant: "soft",
    icon: /*#__PURE__*/React.createElement(Icon, {
      name: "dumbbell",
      size: 12
    })
  }, "For\xE7a +1")), /*#__PURE__*/React.createElement("p", {
    style: {
      fontSize: 13,
      color: 'var(--text-tertiary)',
      marginTop: 26
    }
  }, "Toque para continuar"));
}
Object.assign(window, {
  Home,
  Profile,
  Workout,
  LevelUp,
  todayQuests
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/screens-main.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/screens-onboarding.jsx
try { (() => {
/* Awaken UI kit — entry flow: Splash → Plans (up-front) → Onboarding. */
const Icon = window.AwakenIcon;
const {
  Button,
  Badge,
  Chip,
  Card
} = window.AwakenDesignSystem_956798;
const {
  StatusBar
} = window;

/* ---------- Splash ---------- */
function Splash({
  onStart
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      background: 'var(--grad-void)'
    }
  }, /*#__PURE__*/React.createElement(StatusBar, null), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '0 32px',
      textAlign: 'center'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      marginBottom: 30
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: -40,
      background: 'radial-gradient(circle, rgba(45,111,245,0.35), transparent 65%)',
      filter: 'blur(8px)'
    }
  }), /*#__PURE__*/React.createElement("img", {
    src: "../../assets/logo-mark.png",
    alt: "Awaken",
    style: {
      width: 200,
      position: 'relative',
      display: 'block'
    }
  })), /*#__PURE__*/React.createElement("img", {
    src: "../../assets/logo-wordmark.png",
    alt: "AWAKEN",
    style: {
      width: 230,
      marginBottom: 22
    }
  }), /*#__PURE__*/React.createElement("p", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 17,
      fontWeight: 500,
      letterSpacing: '0.04em',
      color: 'var(--blue-200)',
      margin: 0,
      textTransform: 'uppercase'
    }
  }, "Desperte o seu potencial"), /*#__PURE__*/React.createElement("p", {
    style: {
      fontSize: 14.5,
      lineHeight: 1.6,
      color: 'var(--text-tertiary)',
      maxWidth: 290,
      marginTop: 14
    }
  }, "A academia \xE9 o dungeon. Voc\xEA \xE9 o hunter. Cada treino \xE9 uma quest.")), /*#__PURE__*/React.createElement("div", {
    style: {
      padding: '0 24px 40px',
      display: 'flex',
      flexDirection: 'column',
      gap: 12
    }
  }, /*#__PURE__*/React.createElement(Button, {
    variant: "primary",
    size: "lg",
    glow: true,
    fullWidth: true,
    onClick: onStart,
    rightIcon: /*#__PURE__*/React.createElement(Icon, {
      name: "arrowRight",
      size: 18
    })
  }, "Come\xE7ar"), /*#__PURE__*/React.createElement("button", {
    onClick: onStart,
    style: {
      background: 'none',
      border: 'none',
      color: 'var(--text-tertiary)',
      fontSize: 14,
      cursor: 'pointer',
      fontFamily: 'var(--font-body)'
    }
  }, "J\xE1 tenho conta \xB7 ", /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--blue-300)'
    }
  }, "Entrar"))));
}

/* ---------- Plans (shown BEFORE onboarding — honest freemium) ---------- */
function PlanRow({
  icon,
  children,
  on = true
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10,
      fontSize: 13.5,
      color: on ? 'var(--text-secondary)' : 'var(--text-disabled)'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: on ? 'check' : 'x',
    size: 15,
    color: on ? 'var(--success)' : 'var(--text-disabled)',
    strokeWidth: 3
  }), children);
}
function Plans({
  onContinue
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      background: 'var(--grad-void)'
    }
  }, /*#__PURE__*/React.createElement(StatusBar, null), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      overflowY: 'auto',
      padding: '6px 22px 0'
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "eyebrow",
    style: {
      color: 'var(--blue-300)'
    }
  }, "Sem surpresa"), /*#__PURE__*/React.createElement("h1", {
    style: {
      fontSize: 27,
      fontWeight: 700,
      color: 'var(--text-primary)',
      marginTop: 6,
      lineHeight: 1.1
    }
  }, "Veja os planos", /*#__PURE__*/React.createElement("br", null), "antes de come\xE7ar"), /*#__PURE__*/React.createElement("p", {
    style: {
      fontSize: 14,
      color: 'var(--text-tertiary)',
      marginTop: 10,
      marginBottom: 22
    }
  }, "O n\xEDvel free \xE9 de verdade \u2014 funcional e em portugu\xEAs. Fa\xE7a o upgrade quando quiser."), /*#__PURE__*/React.createElement(Card, {
    variant: "glow",
    rank: "S",
    padding: 18,
    style: {
      marginBottom: 14
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 14
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 8
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "crown",
    size: 20,
    color: "var(--gold-400)"
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 18,
      color: 'var(--text-primary)'
    }
  }, "S-Rank")), /*#__PURE__*/React.createElement(Badge, {
    tone: "gold",
    variant: "solid"
  }, "7 dias gr\xE1tis")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 9,
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement(PlanRow, null, "Quests ilimitadas e personalizadas por IA"), /*#__PURE__*/React.createElement(PlanRow, null, "Master Quests semanais com +atributos"), /*#__PURE__*/React.createElement(PlanRow, null, "Nutri\xE7\xE3o completa \xB7 macro tracking"), /*#__PURE__*/React.createElement(PlanRow, null, "Card de perfil animado \xB7 sem an\xFAncios")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'baseline',
      gap: 8,
      marginBottom: 14
    }
  }, /*#__PURE__*/React.createElement("span", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 26,
      color: 'var(--text-primary)'
    }
  }, "R$ 14,90"), /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 13,
      color: 'var(--text-tertiary)'
    }
  }, "/m\xEAs \xB7 ou R$ 99,90/ano")), /*#__PURE__*/React.createElement(Button, {
    variant: "gold",
    size: "md",
    fullWidth: true,
    onClick: onContinue
  }, "Iniciar trial \xB7 sem cart\xE3o")), /*#__PURE__*/React.createElement(Card, {
    padding: 18,
    style: {
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 14
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 17,
      color: 'var(--text-primary)'
    }
  }, "Free Hunter"), /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 13,
      color: 'var(--text-tertiary)'
    }
  }, "Gr\xE1tis pra sempre")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 9,
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement(PlanRow, null, "1 quest di\xE1ria \xB7 sistema de XP e rank completo"), /*#__PURE__*/React.createElement(PlanRow, null, "Card de perfil compartilh\xE1vel"), /*#__PURE__*/React.createElement(PlanRow, {
    on: false
  }, "Quests ilimitadas e Master Quests")), /*#__PURE__*/React.createElement(Button, {
    variant: "secondary",
    size: "md",
    fullWidth: true,
    onClick: onContinue
  }, "Continuar gr\xE1tis"))));
}

/* ---------- Onboarding (respects the user 100%) ---------- */
const STEPS = [{
  key: 'goal',
  q: 'Qual é o seu objetivo?',
  multi: false,
  opts: ['Ganhar massa', 'Perder peso', 'Condicionamento', 'Força pura', 'Manter a forma']
}, {
  key: 'level',
  q: 'Qual o seu nível agora?',
  multi: false,
  opts: ['Iniciante', 'Já treino às vezes', 'Treino há anos']
}, {
  key: 'where',
  q: 'Onde você vai treinar?',
  multi: true,
  opts: ['Em casa', 'Academia', 'Ao ar livre', 'Sem equipamento']
}, {
  key: 'days',
  q: 'Quantos dias por semana?',
  multi: false,
  opts: ['2 dias', '3 dias', '4 dias', '5+ dias']
}];
function Onboarding({
  onDone
}) {
  const [step, setStep] = React.useState(0);
  const [answers, setAnswers] = React.useState({});
  const s = STEPS[step];
  const cur = answers[s.key] || (s.multi ? [] : null);
  const has = s.multi ? cur.length > 0 : !!cur;
  const pick = opt => {
    setAnswers(a => {
      if (s.multi) {
        const arr = a[s.key] || [];
        return {
          ...a,
          [s.key]: arr.includes(opt) ? arr.filter(x => x !== opt) : [...arr, opt]
        };
      }
      return {
        ...a,
        [s.key]: opt
      };
    });
  };
  const next = () => step < STEPS.length - 1 ? setStep(step + 1) : onDone();
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      background: 'var(--grad-void)'
    }
  }, /*#__PURE__*/React.createElement(StatusBar, null), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 14,
      padding: '4px 20px 18px'
    }
  }, /*#__PURE__*/React.createElement("button", {
    onClick: () => step ? setStep(step - 1) : null,
    style: {
      width: 36,
      height: 36,
      borderRadius: 'var(--radius-md)',
      background: 'var(--bg-surface)',
      border: '1px solid var(--border-default)',
      display: 'grid',
      placeItems: 'center',
      color: 'var(--text-secondary)',
      cursor: 'pointer',
      opacity: step ? 1 : 0.35
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "chevronLeft",
    size: 18
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      height: 6,
      borderRadius: 999,
      background: 'var(--ink-700)',
      overflow: 'hidden'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      height: '100%',
      width: `${(step + 1) / STEPS.length * 100}%`,
      background: 'var(--grad-energy)',
      borderRadius: 999,
      transition: 'width var(--dur-base) var(--ease-out)'
    }
  })), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 12,
      color: 'var(--text-tertiary)'
    }
  }, step + 1, "/", STEPS.length)), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      overflowY: 'auto',
      padding: '0 22px'
    }
  }, /*#__PURE__*/React.createElement("h1", {
    style: {
      fontSize: 26,
      fontWeight: 700,
      color: 'var(--text-primary)',
      lineHeight: 1.15,
      marginBottom: 6
    }
  }, s.q), /*#__PURE__*/React.createElement("p", {
    style: {
      fontSize: 13.5,
      color: 'var(--text-tertiary)',
      marginBottom: 22
    }
  }, s.multi ? 'Selecione todas que se aplicam — vamos respeitar 100%.' : 'Escolha uma opção.'), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 11
    }
  }, s.opts.map(opt => {
    const sel = s.multi ? cur.includes(opt) : cur === opt;
    return /*#__PURE__*/React.createElement("button", {
      key: opt,
      onClick: () => pick(opt),
      style: {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '15px 18px',
        borderRadius: 'var(--radius-lg)',
        cursor: 'pointer',
        textAlign: 'left',
        fontFamily: 'var(--font-body)',
        fontSize: 15.5,
        fontWeight: 500,
        color: sel ? '#fff' : 'var(--text-secondary)',
        background: sel ? 'var(--grad-energy-soft), var(--bg-elevated)' : 'var(--bg-surface)',
        border: sel ? '1px solid color-mix(in srgb, var(--blue-400) 55%, transparent)' : '1px solid var(--border-default)',
        boxShadow: sel ? 'var(--glow-blue-sm)' : 'none',
        transition: 'all var(--dur-fast) var(--ease-out)'
      }
    }, opt, sel && /*#__PURE__*/React.createElement(Icon, {
      name: "check",
      size: 18,
      color: "var(--blue-300)",
      strokeWidth: 3
    }));
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      padding: '14px 22px 36px'
    }
  }, /*#__PURE__*/React.createElement(Button, {
    variant: "primary",
    size: "lg",
    glow: has,
    fullWidth: true,
    disabled: !has,
    onClick: next,
    rightIcon: /*#__PURE__*/React.createElement(Icon, {
      name: "arrowRight",
      size: 18
    })
  }, step < STEPS.length - 1 ? 'Continuar' : 'Forjar meu treino')));
}
Object.assign(window, {
  Splash,
  Plans,
  Onboarding
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/screens-onboarding.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/screens-player.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/* Awaken UI kit — Tela do Jogador (Player STATUS window + daily quest system window).
 * Translates the Solo-Leveling "Player status" + "Daily Quest GOAL" reference into the
 * Awaken design language: the System recognizing you as a hunter, your live status, and
 * how the daily quest works (goals → reward, fail → streak reset). */
const Icon = window.AwakenIcon;
const {
  Avatar,
  RankBadge,
  StatBar,
  SystemWindow
} = window.AwakenDesignSystem_956798;
const {
  StatusBar,
  ScreenScroll
} = window;

/* Faceted/notched octagon — the System HUD signature (matches SystemWindow). */
const facet = n => `polygon(${n}px 0, calc(100% - ${n}px) 0, 100% ${n}px, 100% calc(100% - ${n}px), calc(100% - ${n}px) 100%, ${n}px 100%, 0 calc(100% - ${n}px), 0 ${n}px)`;
const SYS = {
  color: 'var(--blue-400)',
  rgb: '77,139,255',
  spark: '#5FE8FF'
};
const PLAYER = {
  name: 'Kael Voss',
  rank: 'B',
  level: 37,
  klass: 'Striker',
  title: 'O Imparável',
  streak: 12,
  vitals: [{
    key: 'vida',
    label: 'Vida',
    cur: 920,
    max: 980,
    color: 'var(--attr-vitality)'
  }, {
    key: 'vigor',
    label: 'Vigor',
    cur: 540,
    max: 700,
    color: 'var(--success)'
  }, {
    key: 'foco',
    label: 'Foco',
    cur: 310,
    max: 420,
    color: SYS.spark
  }],
  attrs: {
    strength: 72,
    agility: 54,
    endurance: 61,
    vitality: 68,
    focus: 45,
    wisdom: 58
  },
  points: 3
};
const ATTR_ORDER = ['strength', 'agility', 'endurance', 'vitality', 'focus', 'wisdom'];
function SectionLabel({
  children,
  color = SYS.color
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10,
      margin: '0 0 12px'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: 6,
      height: 6,
      background: color,
      transform: 'rotate(45deg)',
      flex: 'none'
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 11,
      fontWeight: 700,
      letterSpacing: '0.18em',
      textTransform: 'uppercase',
      color
    }
  }, children), /*#__PURE__*/React.createElement("span", {
    style: {
      flex: 1,
      height: 1,
      background: `linear-gradient(90deg, color-mix(in srgb, ${color} 40%, transparent), transparent)`
    }
  }));
}

/* The faceted System panel shell — reused for the STATUS window. */
function SystemPanel({
  tab,
  children,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      width: '100%',
      filter: `drop-shadow(0 0 30px rgba(${SYS.rgb},0.26))`,
      ...style
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'absolute',
      inset: 0,
      clipPath: facet(16),
      background: `linear-gradient(150deg, ${SYS.color}, rgba(${SYS.rgb},0.15) 45%, ${SYS.color})`
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      clipPath: facet(15),
      margin: 1.5,
      background: `linear-gradient(180deg, color-mix(in srgb, ${SYS.color} 9%, var(--bg-surface)), var(--bg-base))`,
      padding: '18px 20px 20px',
      boxShadow: 'var(--inset-sheen)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      top: 8,
      left: 8,
      width: 14,
      height: 14,
      borderTop: `2px solid ${SYS.color}`,
      borderLeft: `2px solid ${SYS.color}`,
      opacity: 0.7
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      top: 8,
      right: 8,
      width: 14,
      height: 14,
      borderTop: `2px solid ${SYS.color}`,
      borderRight: `2px solid ${SYS.color}`,
      opacity: 0.7
    }
  }), tab && /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'center',
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: 8,
      height: 26,
      padding: '0 16px',
      clipPath: facet(7),
      background: `rgba(${SYS.rgb},0.16)`,
      border: `1px solid color-mix(in srgb, ${SYS.color} 55%, transparent)`
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: 5,
      height: 5,
      background: SYS.color,
      transform: 'rotate(45deg)',
      boxShadow: `0 0 8px ${SYS.color}`
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 12,
      fontWeight: 700,
      letterSpacing: '0.2em',
      textTransform: 'uppercase',
      color: SYS.color
    }
  }, tab))), children));
}

/* RPG vital bar (Vida / Vigor / Foco). */
function VitalBar({
  label,
  cur,
  max,
  color
}) {
  const pct = Math.min(100, Math.round(cur / max * 100));
  return /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'baseline',
      justifyContent: 'space-between',
      marginBottom: 5
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 11,
      fontWeight: 600,
      letterSpacing: '0.1em',
      textTransform: 'uppercase',
      color: 'var(--text-secondary)'
    }
  }, label), /*#__PURE__*/React.createElement("span", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 12,
      color: 'var(--text-tertiary)'
    }
  }, cur, /*#__PURE__*/React.createElement("span", {
    style: {
      opacity: 0.5
    }
  }, "/", max))), /*#__PURE__*/React.createElement("div", {
    style: {
      height: 7,
      borderRadius: 999,
      background: 'rgba(255,255,255,0.07)',
      overflow: 'hidden',
      border: '1px solid var(--border-subtle)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      width: `${pct}%`,
      height: '100%',
      borderRadius: 999,
      background: color,
      boxShadow: `0 0 10px color-mix(in srgb, ${color} 60%, transparent)`
    }
  })));
}
const QUEST_KINDS = [{
  tab: 'Quest Diária',
  color: 'var(--blue-400)',
  rgb: '77,139,255',
  desc: 'O treino do dia. Complete todas as metas ou perca a sua ofensiva.'
}, {
  tab: 'Dungeon',
  color: 'var(--purple-400)',
  rgb: '166,92,238',
  desc: 'Treino pontual avulso. XP extra, sem penalidade de streak.'
}, {
  tab: 'Raid',
  color: 'var(--red-500, #EF4444)',
  rgb: '239,68,68',
  desc: 'Só em grupo. Ativa quando um esquadrão se reúne.'
}];
function PlayerScreen() {
  const a = PLAYER.attrs;
  return /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      background: 'var(--grad-void)'
    }
  }, /*#__PURE__*/React.createElement(StatusBar, null), /*#__PURE__*/React.createElement(ScreenScroll, {
    style: {
      padding: '4px 18px 28px'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 9,
      padding: '9px 13px',
      marginBottom: 16,
      clipPath: facet(8),
      background: `rgba(${SYS.rgb},0.10)`,
      border: `1px solid color-mix(in srgb, ${SYS.color} 32%, transparent)`
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: 7,
      height: 7,
      borderRadius: '50%',
      background: SYS.spark,
      boxShadow: `0 0 8px ${SYS.spark}`,
      flex: 'none'
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 12,
      lineHeight: 1.35,
      color: 'var(--text-secondary)'
    }
  }, "[O Sistema reconheceu voc\xEA como ", /*#__PURE__*/React.createElement("span", {
    style: {
      color: SYS.spark
    }
  }, "Ca\xE7ador"), ".]")), /*#__PURE__*/React.createElement(SystemPanel, {
    tab: "Status",
    style: {
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 14,
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement(Avatar, {
    name: PLAYER.name,
    rank: PLAYER.rank,
    size: 62
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("h2", {
    style: {
      margin: 0,
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 22,
      color: 'var(--text-primary)',
      lineHeight: 1.1
    }
  }, PLAYER.name), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 12,
      color: 'var(--text-tertiary)',
      marginTop: 3
    }
  }, PLAYER.title, " \xB7 Classe ", PLAYER.klass)), /*#__PURE__*/React.createElement(RankBadge, {
    rank: PLAYER.rank,
    size: 44
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 1,
      marginBottom: 18,
      clipPath: facet(8),
      overflow: 'hidden',
      border: `1px solid color-mix(in srgb, ${SYS.color} 28%, transparent)`
    }
  }, [{
    k: 'Rank',
    v: PLAYER.rank
  }, {
    k: 'Nível',
    v: PLAYER.level
  }, {
    k: 'Streak',
    v: PLAYER.streak + ' 🔥'
  }].map(s => /*#__PURE__*/React.createElement("div", {
    key: s.k,
    style: {
      flex: 1,
      textAlign: 'center',
      padding: '10px 4px',
      background: `rgba(${SYS.rgb},0.07)`
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 20,
      color: 'var(--text-primary)'
    }
  }, s.v), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 9.5,
      fontWeight: 600,
      letterSpacing: '0.14em',
      textTransform: 'uppercase',
      color: 'var(--text-tertiary)',
      marginTop: 2
    }
  }, s.k)))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 12,
      marginBottom: 20
    }
  }, PLAYER.vitals.map(v => /*#__PURE__*/React.createElement(VitalBar, _extends({
    key: v.key
  }, v)))), /*#__PURE__*/React.createElement(SectionLabel, null, "Atributos"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 13,
      marginBottom: 16
    }
  }, ATTR_ORDER.map(k => /*#__PURE__*/React.createElement(StatBar, {
    key: k,
    attr: k,
    value: a[k]
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '11px 14px',
      clipPath: facet(8),
      background: 'rgba(245,197,24,0.09)',
      border: '1px solid color-mix(in srgb, var(--gold-500) 40%, transparent)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 9
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "plus",
    size: 16,
    color: "var(--gold-400)"
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 13,
      color: 'var(--text-secondary)'
    }
  }, "Pontos dispon\xEDveis")), /*#__PURE__*/React.createElement("span", {
    className: "tnum",
    style: {
      fontFamily: 'var(--font-display)',
      fontWeight: 700,
      fontSize: 18,
      color: 'var(--gold-400)'
    }
  }, PLAYER.points))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'center',
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement(SystemWindow, {
    kind: "daily",
    title: "Treino do Dia \u2014 For\xE7a",
    rank: "C",
    goals: [{
      label: 'Flexões',
      current: 40,
      target: 100
    }, {
      label: 'Abdominais',
      current: 65,
      target: 100
    }, {
      label: 'Agachamentos',
      current: 30,
      target: 100
    }, {
      label: 'Corrida',
      current: 3.2,
      target: 10,
      unit: 'km'
    }],
    xp: 320,
    rewards: [{
      attr: 'strength',
      amount: 2
    }, {
      attr: 'endurance',
      amount: 1
    }],
    cta: "Continuar Quest"
  })), /*#__PURE__*/React.createElement(SectionLabel, {
    color: "var(--text-secondary)"
  }, "Como funcionam as quests"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 9
    }
  }, QUEST_KINDS.map(q => /*#__PURE__*/React.createElement("div", {
    key: q.tab,
    style: {
      display: 'flex',
      gap: 12,
      alignItems: 'flex-start',
      padding: '12px 14px',
      borderRadius: 'var(--radius-md)',
      background: 'var(--bg-surface)',
      border: '1px solid var(--border-default)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: 9,
      height: 9,
      marginTop: 4,
      flex: 'none',
      background: q.color,
      transform: 'rotate(45deg)',
      boxShadow: `0 0 9px color-mix(in srgb, ${q.color} 70%, transparent)`
    }
  }), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-display)',
      fontSize: 12,
      fontWeight: 700,
      letterSpacing: '0.08em',
      textTransform: 'uppercase',
      color: q.color,
      marginBottom: 2
    }
  }, q.tab), /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 0,
      fontSize: 12.5,
      lineHeight: 1.45,
      color: 'var(--text-tertiary)'
    }
  }, q.desc))))), /*#__PURE__*/React.createElement("p", {
    style: {
      margin: '14px 4px 0',
      fontSize: 12,
      lineHeight: 1.5,
      color: 'var(--text-tertiary)'
    }
  }, "Cada meta conclu\xEDda enche a sua barra de progresso. A recompensa (XP + atributos) s\xF3 \xE9 concedida quando ", /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "todas as metas"), " s\xE3o batidas. Falhar a Quest Di\xE1ria reinicia a sua ofensiva \uD83D\uDD25.")));
}
window.PlayerScreen = PlayerScreen;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/screens-player.jsx", error: String((e && e.message) || e) }); }

__ds_ns.Avatar = __ds_scope.Avatar;

__ds_ns.Badge = __ds_scope.Badge;

__ds_ns.Button = __ds_scope.Button;

__ds_ns.Card = __ds_scope.Card;

__ds_ns.Chip = __ds_scope.Chip;

__ds_ns.Input = __ds_scope.Input;

__ds_ns.Switch = __ds_scope.Switch;

__ds_ns.ProgressRing = __ds_scope.ProgressRing;

__ds_ns.QuestCard = __ds_scope.QuestCard;

__ds_ns.RankBadge = __ds_scope.RankBadge;

__ds_ns.StatBar = __ds_scope.StatBar;

__ds_ns.SystemWindow = __ds_scope.SystemWindow;

__ds_ns.XPBar = __ds_scope.XPBar;

})();
