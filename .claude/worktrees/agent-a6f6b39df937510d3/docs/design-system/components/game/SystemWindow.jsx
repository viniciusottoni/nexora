import React from 'react';

/* Quest taxonomy → theme. Daily=blue, Dungeon=purple, Raid=red/gold. */
const KINDS = {
  daily: {
    label: 'Quest Diária',
    color: 'var(--blue-400)',
    rgb: '77,139,255',
    glow: 'var(--glow-blue)',
  },
  dungeon: {
    label: 'Dungeon',
    color: 'var(--purple-400)',
    rgb: '166,92,238',
    glow: 'var(--glow-purple)',
  },
  raid: {
    label: 'Raid',
    color: 'var(--red-500, #EF4444)',
    rgb: '239,68,68',
    glow: 'var(--glow-danger)',
  },
};

const ATTRS = {
  strength:  { label: 'Força',      color: 'var(--attr-strength)' },
  agility:   { label: 'Agilidade',  color: 'var(--attr-agility)' },
  endurance: { label: 'Resistência', color: 'var(--attr-endurance)' },
  vitality:  { label: 'Vitalidade', color: 'var(--attr-vitality)' },
  focus:     { label: 'Foco',       color: 'var(--attr-focus)' },
  wisdom:    { label: 'Sabedoria',  color: 'var(--attr-wisdom)' },
};

/* Notched / faceted octagon — the HUD signature. */
const facet = (n) =>
  `polygon(${n}px 0, calc(100% - ${n}px) 0, 100% ${n}px, 100% calc(100% - ${n}px), calc(100% - ${n}px) 100%, ${n}px 100%, 0 calc(100% - ${n}px), 0 ${n}px)`;

function SectionLabel({ children, color }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10, margin: '0 0 11px' }}>
      <span style={{ width: 6, height: 6, background: color, transform: 'rotate(45deg)', flex: 'none' }} />
      <span style={{
        fontFamily: 'var(--font-display)', fontSize: 11, fontWeight: 700,
        letterSpacing: '0.18em', textTransform: 'uppercase', color,
      }}>{children}</span>
      <span style={{ flex: 1, height: 1, background: `linear-gradient(90deg, color-mix(in srgb, ${color} 40%, transparent), transparent)` }} />
    </div>
  );
}

/**
 * Awaken SystemWindow — the "System" HUD panel that announces a quest. A faceted,
 * glowing window themed by quest kind (daily / dungeon / raid) showing goals,
 * rewards (XP + 1-2 attribute points) and, for daily quests, a penalty warning.
 */
export function SystemWindow({
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
  const warnText = typeof warning === 'string'
    ? warning
    : 'A recompensa diária é concedida apenas ao completar todas as metas. Quests diárias não cumpridas reiniciam a sua ofensiva (streak).';

  return (
    <div
      style={{
        position: 'relative',
        width: '100%',
        maxWidth: 420,
        fontFamily: 'var(--font-body)',
        filter: `drop-shadow(0 0 30px rgba(${k.rgb},0.28))`,
        ...style,
      }}
      {...rest}
    >
      {/* Outer faceted glow border */}
      <div style={{
        position: 'absolute', inset: 0, clipPath: facet(16),
        background: `linear-gradient(150deg, ${k.color}, rgba(${k.rgb},0.15) 45%, ${k.color})`,
      }} />
      {/* Inner dark panel */}
      <div style={{
        position: 'relative', clipPath: facet(15), margin: 1.5,
        background: `linear-gradient(180deg, color-mix(in srgb, ${k.color} 9%, var(--bg-surface)), var(--bg-base))`,
        padding: '20px 22px 22px',
        boxShadow: 'var(--inset-sheen)',
      }}>
        {/* corner ticks */}
        <span style={{ position: 'absolute', top: 8, left: 8, width: 14, height: 14, borderTop: `2px solid ${k.color}`, borderLeft: `2px solid ${k.color}`, opacity: 0.7 }} />
        <span style={{ position: 'absolute', top: 8, right: 8, width: 14, height: 14, borderTop: `2px solid ${k.color}`, borderRight: `2px solid ${k.color}`, opacity: 0.7 }} />

        {/* Header — kind tab */}
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginBottom: 16 }}>
          <div style={{
            display: 'inline-flex', alignItems: 'center', gap: 8, height: 26, padding: '0 16px',
            clipPath: facet(7),
            background: `rgba(${k.rgb},0.16)`,
            border: `1px solid color-mix(in srgb, ${k.color} 55%, transparent)`,
          }}>
            <span style={{ width: 5, height: 5, background: k.color, transform: 'rotate(45deg)', boxShadow: `0 0 8px ${k.color}` }} />
            <span style={{
              fontFamily: 'var(--font-display)', fontSize: 12, fontWeight: 700,
              letterSpacing: '0.2em', textTransform: 'uppercase', color: k.color,
            }}>{k.label}</span>
          </div>
        </div>

        {/* Title + optional rank */}
        <div style={{ textAlign: 'center', marginBottom: description ? 6 : 18 }}>
          <h3 style={{
            margin: 0, fontFamily: 'var(--font-display)', fontWeight: 700,
            fontSize: 23, lineHeight: 1.12, color: 'var(--text-primary)',
            textShadow: `0 0 18px rgba(${k.rgb},0.25)`,
          }}>{title}</h3>
        </div>
        {description && (
          <p style={{ margin: '0 0 18px', textAlign: 'center', fontSize: 13, lineHeight: 1.5, color: 'var(--text-tertiary)' }}>{description}</p>
        )}
        {rank && (
          <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 18 }}>
            <span style={{
              fontFamily: 'var(--font-display)', fontSize: 11, fontWeight: 600, letterSpacing: '0.12em',
              textTransform: 'uppercase', color: 'var(--text-tertiary)',
            }}>Dificuldade&nbsp;·&nbsp;<span style={{ color: k.color, fontWeight: 700 }}>Rank {rank}</span></span>
          </div>
        )}

        {/* GOALS */}
        {goals.length > 0 && (
          <div style={{ marginBottom: 18 }}>
            <SectionLabel color={k.color}>Goal</SectionLabel>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 11 }}>
              {goals.map((g, i) => {
                const pct = g.target ? Math.min(100, Math.round((g.current / g.target) * 100)) : (g.done ? 100 : 0);
                const done = pct >= 100;
                return (
                  <div key={i}>
                    <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', marginBottom: 6 }}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 14, color: done ? 'var(--text-secondary)' : 'var(--text-primary)' }}>
                        {done && (
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="var(--success)" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
                        )}
                        {g.label}
                      </span>
                      <span style={{
                        fontFamily: 'var(--font-display)', fontSize: 13, fontWeight: 600, fontVariantNumeric: 'tabular-nums',
                        color: done ? 'var(--success)' : k.color,
                      }}>
                        {g.target != null ? `${g.current}/${g.target}` : (done ? 'OK' : '—')}{g.unit ? ` ${g.unit}` : ''}
                      </span>
                    </div>
                    <div style={{ height: 4, borderRadius: 999, background: 'rgba(255,255,255,0.07)', overflow: 'hidden' }}>
                      <div style={{ width: `${pct}%`, height: '100%', borderRadius: 999, background: done ? 'var(--success)' : k.color, boxShadow: done ? 'none' : `0 0 8px ${k.color}` }} />
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {/* REWARDS */}
        {(xp > 0 || rewards.length > 0) && (
          <div style={{ marginBottom: showWarning || cta ? 18 : 0 }}>
            <SectionLabel color={k.color}>Recompensa</SectionLabel>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
              {xp > 0 && (
                <span style={{
                  display: 'inline-flex', alignItems: 'center', gap: 6, height: 30, padding: '0 12px', clipPath: facet(6),
                  background: 'rgba(245,197,24,0.12)', border: '1px solid color-mix(in srgb, var(--gold-500) 45%, transparent)',
                  fontFamily: 'var(--font-display)', fontSize: 13, fontWeight: 700, color: 'var(--gold-400)', fontVariantNumeric: 'tabular-nums',
                }}>
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="var(--gold-400)" stroke="none"><path d="M13 2 3 14h7l-1 8 10-12h-7z" /></svg>
                  +{xp} XP
                </span>
              )}
              {rewards.map((r, i) => {
                const a = ATTRS[r.attr] || ATTRS.strength;
                return (
                  <span key={i} style={{
                    display: 'inline-flex', alignItems: 'center', gap: 6, height: 30, padding: '0 12px', clipPath: facet(6),
                    background: `color-mix(in srgb, ${a.color} 13%, transparent)`,
                    border: `1px solid color-mix(in srgb, ${a.color} 45%, transparent)`,
                    fontFamily: 'var(--font-display)', fontSize: 13, fontWeight: 700, color: a.color, fontVariantNumeric: 'tabular-nums',
                  }}>
                    <span style={{ width: 6, height: 6, background: a.color, transform: 'rotate(45deg)' }} />
                    +{r.amount} {a.label}
                  </span>
                );
              })}
            </div>
          </div>
        )}

        {/* PARTICIPANTS (raid) */}
        {participants && (
          <div style={{ marginBottom: showWarning || cta ? 18 : 0 }}>
            <SectionLabel color={k.color}>Esquadrão</SectionLabel>
            <span style={{ fontSize: 13, color: 'var(--text-secondary)', fontVariantNumeric: 'tabular-nums' }}>
              {participants.current}/{participants.max} caçadores reunidos
            </span>
          </div>
        )}

        {/* WARNING (daily only) */}
        {showWarning && (
          <div style={{
            display: 'flex', alignItems: 'flex-start', gap: 10, padding: '12px 13px', clipPath: facet(8),
            background: 'rgba(239,68,68,0.08)', border: '1px solid color-mix(in srgb, var(--danger) 38%, transparent)',
          }}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z" /><path d="M12 9v4" /><path d="M12 17h.01" /></svg>
            <div>
              <div style={{ fontFamily: 'var(--font-display)', fontSize: 11, fontWeight: 700, letterSpacing: '0.16em', textTransform: 'uppercase', color: 'var(--danger)', marginBottom: 3 }}>Aviso</div>
              <p style={{ margin: 0, fontSize: 12.5, lineHeight: 1.45, color: 'var(--text-secondary)' }}>{warnText}</p>
            </div>
          </div>
        )}

        {/* CTA */}
        {cta && (
          <button
            onClick={onCta}
            style={{
              marginTop: 18, width: '100%', height: 46, clipPath: facet(8), cursor: 'pointer',
              fontFamily: 'var(--font-display)', fontSize: 14, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase',
              color: 'var(--text-on-primary)', border: 'none',
              background: `linear-gradient(180deg, ${k.color}, color-mix(in srgb, ${k.color} 70%, #000))`,
              boxShadow: k.glow,
            }}
          >{cta}</button>
        )}
      </div>
    </div>
  );
}
