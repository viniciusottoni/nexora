import React from 'react';
import { Badge } from '../core/Badge.jsx';

const ATTR_TONE = { strength: 'red', agility: 'green', endurance: 'blue', vitality: 'gold', focus: 'purple', wisdom: 'cyan' };
const ATTR_LABEL = { strength: 'Força', agility: 'Agilidade', endurance: 'Resistência', vitality: 'Vitalidade', focus: 'Foco', wisdom: 'Sabedoria' };

/**
 * Awaken QuestCard — a daily quest row. The product's core loop unit: title, the
 * attribute it trains, the XP reward, and a completion state.
 */
export function QuestCard({
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
  const rewardList = (rewards && rewards.length)
    ? rewards
    : (attr ? [{ attr, amount: null }] : []);

  return (
    <div
      onClick={onClick}
      style={{
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
        ...style,
      }}
      {...rest}
    >
      {/* completion toggle */}
      <button
        type="button"
        onClick={(e) => { e.stopPropagation(); onToggle && onToggle(); }}
        aria-pressed={done}
        style={{
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
          boxShadow: done ? '0 0 10px rgba(34,197,94,0.35)' : 'none',
        }}
      >
        {done && (
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3.5" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
        )}
      </button>

      {icon && (
        <div style={{ flexShrink: 0, width: 40, height: 40, clipPath: 'polygon(6px 0,100% 0,100% calc(100% - 6px),calc(100% - 6px) 100%,0 100%,0 6px)', display: 'grid', placeItems: 'center', background: 'var(--bg-elevated)', color: 'var(--blue-300)' }}>
          {icon}
        </div>
      )}

      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontFamily: 'var(--font-body)', fontWeight: 600, fontSize: 15, color: 'var(--text-primary)', textDecoration: done ? 'line-through' : 'none' }}>
          {title}
        </div>
        {subtitle && (
          <div style={{ fontSize: 13, color: 'var(--text-tertiary)', marginTop: 2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{subtitle}</div>
        )}
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 8 }}>
          {rewardList.map((r, i) => (
            <Badge key={i} tone={ATTR_TONE[r.attr]} variant="soft">
              {r.amount != null ? `+${r.amount} ` : ''}{ATTR_LABEL[r.attr]}
            </Badge>
          ))}
          {active && <Badge tone="cyan" variant="outline">Em andamento</Badge>}
        </div>
      </div>

      <div style={{ flexShrink: 0, textAlign: 'right' }}>
        <div className="tnum" style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 16, color: 'var(--gold-400)', lineHeight: 1 }}>+{xp}</div>
        <div style={{ fontFamily: 'var(--font-mono)', fontSize: 10, letterSpacing: '0.1em', color: 'var(--text-tertiary)', marginTop: 3 }}>XP</div>
      </div>
    </div>
  );
}
