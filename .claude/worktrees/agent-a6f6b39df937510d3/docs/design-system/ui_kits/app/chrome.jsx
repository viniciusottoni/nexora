/* Awaken UI kit — shared chrome: phone frame, status bar, tab bar, screen header. */
const Icon = window.AwakenIcon;

function StatusBar({ dark = false }) {
  return (
    <div style={{ height: 44, display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      padding: '0 24px', flexShrink: 0, color: 'var(--text-primary)', fontFamily: 'var(--font-mono)', fontSize: 13, fontWeight: 500 }}>
      <span className="tnum">9:41</span>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <svg width="18" height="12" viewBox="0 0 18 12" fill="currentColor"><rect x="0" y="7" width="3" height="5" rx="1"/><rect x="5" y="4" width="3" height="8" rx="1"/><rect x="10" y="1.5" width="3" height="10.5" rx="1" opacity="0.5"/><rect x="15" y="0" width="3" height="12" rx="1" opacity="0.3"/></svg>
        <svg width="16" height="12" viewBox="0 0 16 12" fill="none" stroke="currentColor" strokeWidth="1.4"><path d="M1 4.5C4.5 1.5 11.5 1.5 15 4.5M3 7C5.5 5 10.5 5 13 7M5.5 9.3C7 8.2 9 8.2 10.5 9.3"/></svg>
        <svg width="24" height="12" viewBox="0 0 24 12" fill="none"><rect x="1" y="1" width="20" height="10" rx="2.5" stroke="currentColor" strokeOpacity="0.4"/><rect x="3" y="3" width="15" height="6" rx="1" fill="currentColor"/><rect x="22" y="4" width="1.5" height="4" rx="0.75" fill="currentColor" fillOpacity="0.4"/></svg>
      </div>
    </div>
  );
}

function PhoneFrame({ children, glow = true }) {
  return (
    <div style={{ width: 390, height: 'min(844px, 92vh)', maxHeight: 844, borderRadius: 46, padding: 5,
      background: 'linear-gradient(160deg, #23263a, #0d0f18)',
      boxShadow: glow
        ? '0 40px 100px rgba(0,0,0,0.6), 0 0 0 1px rgba(255,255,255,0.05), 0 0 90px rgba(45,111,245,0.18)'
        : '0 40px 100px rgba(0,0,0,0.6)',
      flexShrink: 0 }}>
      <div style={{ width: '100%', height: '100%', borderRadius: 42, overflow: 'hidden', position: 'relative',
        background: 'var(--bg-base)', display: 'flex', flexDirection: 'column' }}>
        <div style={{ position: 'absolute', top: 9, left: '50%', transform: 'translateX(-50%)', width: 116, height: 30,
          background: '#000', borderRadius: 999, zIndex: 50 }} />
        {children}
      </div>
    </div>
  );
}

function ScreenScroll({ children, style }) {
  return (
    <div style={{ flex: 1, overflowY: 'auto', overflowX: 'hidden', WebkitOverflowScrolling: 'touch', ...style }}>
      {children}
    </div>
  );
}

function AppHeader({ title, eyebrow, left, right }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12,
      padding: '6px 20px 14px' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, minWidth: 0 }}>
        {left}
        <div style={{ minWidth: 0 }}>
          {eyebrow && <div className="eyebrow" style={{ marginBottom: 2 }}>{eyebrow}</div>}
          <h1 style={{ fontSize: 22, fontWeight: 700, color: 'var(--text-primary)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{title}</h1>
        </div>
      </div>
      {right}
    </div>
  );
}

function IconBtn({ name, onClick, badge }) {
  return (
    <button onClick={onClick} style={{ position: 'relative', width: 40, height: 40, borderRadius: 'var(--radius-md)',
      display: 'grid', placeItems: 'center', background: 'var(--bg-surface)', border: '1px solid var(--border-default)',
      color: 'var(--text-secondary)', cursor: 'pointer', flexShrink: 0 }}>
      <Icon name={name} size={19} />
      {badge && <span style={{ position: 'absolute', top: 8, right: 8, width: 7, height: 7, borderRadius: '50%', background: 'var(--danger)', boxShadow: '0 0 6px var(--danger)' }} />}
    </button>
  );
}

function TabBar({ active, onChange, onTrain }) {
  const Tab = ({ id, icon, label }) => {
    const on = active === id;
    return (
      <button onClick={() => onChange(id)} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center',
        gap: 4, padding: '8px 0', background: 'none', border: 'none', cursor: 'pointer',
        color: on ? 'var(--blue-300)' : 'var(--text-tertiary)' }}>
        <Icon name={icon} size={22} strokeWidth={on ? 2.4 : 2} />
        <span style={{ fontFamily: 'var(--font-display)', fontSize: 10, fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase' }}>{label}</span>
      </button>
    );
  };
  return (
    <div style={{ position: 'relative', flexShrink: 0, display: 'flex', alignItems: 'flex-end',
      padding: '0 14px 22px', background: 'linear-gradient(0deg, var(--bg-base) 60%, transparent)',
      borderTop: '1px solid var(--border-subtle)' }}>
      <Tab id="home" icon="home" label="Início" />
      <div style={{ width: 76, display: 'flex', justifyContent: 'center', position: 'relative' }}>
        <button onClick={onTrain} style={{ position: 'absolute', bottom: 6, width: 62, height: 62, borderRadius: '50%',
          display: 'grid', placeItems: 'center', background: 'var(--grad-energy)', border: '3px solid var(--bg-base)',
          color: '#fff', cursor: 'pointer', boxShadow: 'var(--glow-blue)' }}>
          <Icon name="swords" size={26} strokeWidth={2.2} />
        </button>
      </div>
      <Tab id="profile" icon="user" label="Perfil" />
    </div>
  );
}

Object.assign(window, { StatusBar, PhoneFrame, ScreenScroll, AppHeader, IconBtn, TabBar });
