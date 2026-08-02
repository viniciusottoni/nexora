// Awaken UI Atoms — RankBadge · XPBar · StatBar
// Exports to window.* for use in app.jsx

const ATTR_CONFIG = {
  strength:  { label: 'Força',       color: '#FF5A3C' },
  agility:   { label: 'Agilidade',   color: '#22D3A7' },
  endurance: { label: 'Resistência', color: '#2D6FF5' },
  vitality:  { label: 'Vitalidade',  color: '#F5C518' },
  focus:     { label: 'Foco',        color: '#A65CEE' },
  wisdom:    { label: 'Sabedoria',   color: '#5FE8FF' },
};

const RARITY_CONFIG = {
  common:     { label: 'Comum',      color: '#6B7280' },
  uncommon:   { label: 'Incomum',    color: '#22C55E' },
  rare:       { label: 'Raro',       color: '#3B82F6' },
  epic:       { label: 'Épico',      color: '#A855F7' },
  legendary:  { label: 'Lendário',   color: '#F5C518' },
  consumable: { label: 'Consumível', color: '#FF9500' },
};

function RankBadge({ rank = 'E', size = 64, glow = true, style }) {
  const RANKS = {
    E:   { color: '#6B7280', grad: 'linear-gradient(160deg,#8A92A3,#4B5160)' },
    D:   { color: '#22C55E', grad: 'linear-gradient(160deg,#4CE07F,#15803D)' },
    C:   { color: '#3B82F6', grad: 'linear-gradient(160deg,#5B9BFF,#1D4ED8)' },
    B:   { color: '#A855F7', grad: 'linear-gradient(160deg,#C07BFF,#7E22CE)' },
    A:   { color: '#EAB308', grad: 'linear-gradient(160deg,#FACC15,#B8860B)' },
    S:   { color: '#EF4444', grad: 'linear-gradient(160deg,#FF6B5B,#C81E2C)' },
    SS:  { color: '#FF5EAD', grad: 'linear-gradient(135deg,#EF4444,#FF5EAD,#F5C518)' },
    SSS: { color: '#5FE8FF', grad: 'linear-gradient(135deg,#5FE8FF,#8B3FD8,#F5C518)' },
  };
  const r = RANKS[rank] || RANKS.E;
  const d = size * 0.09;
  const ins = size * 0.06;
  const corners = [
    { top: -d/2, left: -d/2 },
    { top: -d/2, left: size - d/2 },
    { top: size - d/2, left: -d/2 },
    { top: size - d/2, left: size - d/2 },
  ];
  return (
    <div style={{
      position: 'relative', width: size, height: size,
      display: 'inline-flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
      filter: glow ? `drop-shadow(0 0 ${size * 0.22}px color-mix(in srgb,${r.color} 65%,transparent))` : 'none',
      ...(style || {}),
    }}>
      <div style={{ position: 'absolute', inset: 0, background: r.grad, transform: 'rotate(45deg)' }} />
      <div style={{ position: 'absolute', inset: ins, background: '#0A0B12', transform: 'rotate(45deg)' }} />
      <div style={{ position: 'absolute', inset: ins, background: `linear-gradient(180deg,color-mix(in srgb,${r.color} 18%,transparent),transparent 55%)`, transform: 'rotate(45deg)' }} />
      {corners.map((p, i) => (
        <span key={i} style={{ position: 'absolute', ...p, width: d, height: d, background: r.color, transform: 'rotate(45deg)', boxShadow: `0 0 ${d * 2}px ${r.color}` }} />
      ))}
      <span style={{
        position: 'relative',
        fontFamily: "'Chakra Petch', sans-serif", fontWeight: 700,
        fontSize: size * (rank.length === 3 ? 0.28 : rank.length === 2 ? 0.36 : 0.46),
        letterSpacing: '-0.01em', lineHeight: 1, color: r.color,
        textShadow: `0 0 ${size * 0.14}px color-mix(in srgb,${r.color} 80%,transparent)`,
      }}>{rank}</span>
    </div>
  );
}

function XPBar({ value = 0, max = 100, level, height = 10, style }) {
  const pct = Math.max(0, Math.min(100, (value / max) * 100));
  return (
    <div style={style || {}}>
      <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', marginBottom: 6 }}>
        {level != null && (
          <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontWeight: 600, fontSize: 12, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#FFD64A' }}>
            Nível {level}
          </span>
        )}
        <span style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 11, color: '#828AAE' }}>
          {Math.round(value)} / {max} XP
        </span>
      </div>
      <div style={{ position: 'relative', height, borderRadius: 999, background: '#1D2133', overflow: 'hidden', boxShadow: 'inset 0 1px 2px rgba(0,0,0,0.5)' }}>
        <div style={{ position: 'absolute', inset: 0, width: `${pct}%`, borderRadius: 999, background: 'linear-gradient(90deg,#F5C518,#FF9500)', boxShadow: '0 0 14px rgba(245,197,24,0.55)', transition: 'width 600ms cubic-bezier(0.16,1,0.3,1)' }}>
          <span style={{ position: 'absolute', inset: 0, background: 'linear-gradient(180deg,rgba(255,255,255,0.45),transparent 60%)', borderRadius: 'inherit' }} />
        </div>
      </div>
    </div>
  );
}

function StatBar({ attr = 'strength', value = 0, max = 100, style }) {
  const a = ATTR_CONFIG[attr] || ATTR_CONFIG.strength;
  const pct = Math.max(0, Math.min(100, (value / max) * 100));
  return (
    <div style={style || {}}>
      <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', marginBottom: 5 }}>
        <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 11, fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#AEB4D0' }}>{a.label}</span>
        <span style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 13, fontWeight: 700, color: a.color }}>{Math.round(value)}</span>
      </div>
      <div style={{ height: 7, borderRadius: 999, background: '#1D2133', overflow: 'hidden' }}>
        <div style={{ height: '100%', width: `${pct}%`, borderRadius: 999, background: `linear-gradient(90deg,color-mix(in srgb,${a.color} 70%,#000),${a.color})`, boxShadow: `0 0 8px color-mix(in srgb,${a.color} 55%,transparent)`, transition: 'width 360ms cubic-bezier(0.16,1,0.3,1)' }} />
      </div>
    </div>
  );
}

Object.assign(window, { RankBadge, XPBar, StatBar, ATTR_CONFIG, RARITY_CONFIG });
