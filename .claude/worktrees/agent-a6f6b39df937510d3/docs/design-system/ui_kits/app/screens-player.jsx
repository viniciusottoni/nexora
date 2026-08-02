/* Awaken UI kit — Tela do Jogador (Player STATUS window + daily quest system window).
 * Translates the Solo-Leveling "Player status" + "Daily Quest GOAL" reference into the
 * Awaken design language: the System recognizing you as a hunter, your live status, and
 * how the daily quest works (goals → reward, fail → streak reset). */
const Icon = window.AwakenIcon;
const { Avatar, RankBadge, StatBar, SystemWindow } = window.AwakenDesignSystem_956798;
const { StatusBar, ScreenScroll } = window;

/* Faceted/notched octagon — the System HUD signature (matches SystemWindow). */
const facet = (n) =>
  `polygon(${n}px 0, calc(100% - ${n}px) 0, 100% ${n}px, 100% calc(100% - ${n}px), calc(100% - ${n}px) 100%, ${n}px 100%, 0 calc(100% - ${n}px), 0 ${n}px)`;

const SYS = { color: 'var(--blue-400)', rgb: '77,139,255', spark: '#5FE8FF' };

const PLAYER = {
  name: 'Kael Voss', rank: 'B', level: 37, klass: 'Striker', title: 'O Imparável', streak: 12,
  vitals: [
    { key: 'vida',  label: 'Vida',  cur: 920, max: 980, color: 'var(--attr-vitality)' },
    { key: 'vigor', label: 'Vigor', cur: 540, max: 700, color: 'var(--success)' },
    { key: 'foco',  label: 'Foco',  cur: 310, max: 420, color: SYS.spark },
  ],
  attrs: { strength: 72, agility: 54, endurance: 61, vitality: 68, focus: 45, wisdom: 58 },
  points: 3,
};
const ATTR_ORDER = ['strength', 'agility', 'endurance', 'vitality', 'focus', 'wisdom'];

function SectionLabel({ children, color = SYS.color }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10, margin: '0 0 12px' }}>
      <span style={{ width: 6, height: 6, background: color, transform: 'rotate(45deg)', flex: 'none' }} />
      <span style={{ fontFamily: 'var(--font-display)', fontSize: 11, fontWeight: 700, letterSpacing: '0.18em', textTransform: 'uppercase', color }}>{children}</span>
      <span style={{ flex: 1, height: 1, background: `linear-gradient(90deg, color-mix(in srgb, ${color} 40%, transparent), transparent)` }} />
    </div>
  );
}

/* The faceted System panel shell — reused for the STATUS window. */
function SystemPanel({ tab, children, style }) {
  return (
    <div style={{ position: 'relative', width: '100%', filter: `drop-shadow(0 0 30px rgba(${SYS.rgb},0.26))`, ...style }}>
      <div style={{ position: 'absolute', inset: 0, clipPath: facet(16),
        background: `linear-gradient(150deg, ${SYS.color}, rgba(${SYS.rgb},0.15) 45%, ${SYS.color})` }} />
      <div style={{ position: 'relative', clipPath: facet(15), margin: 1.5,
        background: `linear-gradient(180deg, color-mix(in srgb, ${SYS.color} 9%, var(--bg-surface)), var(--bg-base))`,
        padding: '18px 20px 20px', boxShadow: 'var(--inset-sheen)' }}>
        <span style={{ position: 'absolute', top: 8, left: 8, width: 14, height: 14, borderTop: `2px solid ${SYS.color}`, borderLeft: `2px solid ${SYS.color}`, opacity: 0.7 }} />
        <span style={{ position: 'absolute', top: 8, right: 8, width: 14, height: 14, borderTop: `2px solid ${SYS.color}`, borderRight: `2px solid ${SYS.color}`, opacity: 0.7 }} />
        {tab && (
          <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 18 }}>
            <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8, height: 26, padding: '0 16px', clipPath: facet(7),
              background: `rgba(${SYS.rgb},0.16)`, border: `1px solid color-mix(in srgb, ${SYS.color} 55%, transparent)` }}>
              <span style={{ width: 5, height: 5, background: SYS.color, transform: 'rotate(45deg)', boxShadow: `0 0 8px ${SYS.color}` }} />
              <span style={{ fontFamily: 'var(--font-display)', fontSize: 12, fontWeight: 700, letterSpacing: '0.2em', textTransform: 'uppercase', color: SYS.color }}>{tab}</span>
            </div>
          </div>
        )}
        {children}
      </div>
    </div>
  );
}

/* RPG vital bar (Vida / Vigor / Foco). */
function VitalBar({ label, cur, max, color }) {
  const pct = Math.min(100, Math.round((cur / max) * 100));
  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', marginBottom: 5 }}>
        <span style={{ fontFamily: 'var(--font-display)', fontSize: 11, fontWeight: 600, letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--text-secondary)' }}>{label}</span>
        <span className="tnum" style={{ fontFamily: 'var(--font-mono)', fontSize: 12, color: 'var(--text-tertiary)' }}>{cur}<span style={{ opacity: 0.5 }}>/{max}</span></span>
      </div>
      <div style={{ height: 7, borderRadius: 999, background: 'rgba(255,255,255,0.07)', overflow: 'hidden', border: '1px solid var(--border-subtle)' }}>
        <div style={{ width: `${pct}%`, height: '100%', borderRadius: 999, background: color, boxShadow: `0 0 10px color-mix(in srgb, ${color} 60%, transparent)` }} />
      </div>
    </div>
  );
}

const QUEST_KINDS = [
  { tab: 'Quest Diária', color: 'var(--blue-400)', rgb: '77,139,255', desc: 'O treino do dia. Complete todas as metas ou perca a sua ofensiva.' },
  { tab: 'Dungeon', color: 'var(--purple-400)', rgb: '166,92,238', desc: 'Treino pontual avulso. XP extra, sem penalidade de streak.' },
  { tab: 'Raid', color: 'var(--red-500, #EF4444)', rgb: '239,68,68', desc: 'Só em grupo. Ativa quando um esquadrão se reúne.' },
];

function PlayerScreen() {
  const a = PLAYER.attrs;
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: 'var(--grad-void)' }}>
      <StatusBar />
      <ScreenScroll style={{ padding: '4px 18px 28px' }}>
        {/* System notification — "you've become a hunter" */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 9, padding: '9px 13px', marginBottom: 16, clipPath: facet(8),
          background: `rgba(${SYS.rgb},0.10)`, border: `1px solid color-mix(in srgb, ${SYS.color} 32%, transparent)` }}>
          <span style={{ width: 7, height: 7, borderRadius: '50%', background: SYS.spark, boxShadow: `0 0 8px ${SYS.spark}`, flex: 'none' }} />
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 12, lineHeight: 1.35, color: 'var(--text-secondary)' }}>
            [O Sistema reconheceu você como <span style={{ color: SYS.spark }}>Caçador</span>.]
          </span>
        </div>

        {/* ===== STATUS WINDOW ===== */}
        <SystemPanel tab="Status" style={{ marginBottom: 18 }}>
          {/* Identity */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 18 }}>
            <Avatar name={PLAYER.name} rank={PLAYER.rank} size={62} />
            <div style={{ flex: 1, minWidth: 0 }}>
              <h2 style={{ margin: 0, fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 22, color: 'var(--text-primary)', lineHeight: 1.1 }}>{PLAYER.name}</h2>
              <div style={{ fontSize: 12, color: 'var(--text-tertiary)', marginTop: 3 }}>{PLAYER.title} · Classe {PLAYER.klass}</div>
            </div>
            <RankBadge rank={PLAYER.rank} size={44} />
          </div>

          {/* Rank / Level / Streak readout */}
          <div style={{ display: 'flex', gap: 1, marginBottom: 18, clipPath: facet(8), overflow: 'hidden', border: `1px solid color-mix(in srgb, ${SYS.color} 28%, transparent)` }}>
            {[
              { k: 'Rank', v: PLAYER.rank },
              { k: 'Nível', v: PLAYER.level },
              { k: 'Streak', v: PLAYER.streak + ' 🔥' },
            ].map((s) => (
              <div key={s.k} style={{ flex: 1, textAlign: 'center', padding: '10px 4px', background: `rgba(${SYS.rgb},0.07)` }}>
                <div className="tnum" style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 20, color: 'var(--text-primary)' }}>{s.v}</div>
                <div style={{ fontFamily: 'var(--font-display)', fontSize: 9.5, fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--text-tertiary)', marginTop: 2 }}>{s.k}</div>
              </div>
            ))}
          </div>

          {/* Vitals */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginBottom: 20 }}>
            {PLAYER.vitals.map((v) => <VitalBar key={v.key} {...v} />)}
          </div>

          {/* Attributes */}
          <SectionLabel>Atributos</SectionLabel>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 13, marginBottom: 16 }}>
            {ATTR_ORDER.map((k) => <StatBar key={k} attr={k} value={a[k]} />)}
          </div>

          {/* Available points */}
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '11px 14px', clipPath: facet(8),
            background: 'rgba(245,197,24,0.09)', border: '1px solid color-mix(in srgb, var(--gold-500) 40%, transparent)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
              <Icon name="plus" size={16} color="var(--gold-400)" />
              <span style={{ fontSize: 13, color: 'var(--text-secondary)' }}>Pontos disponíveis</span>
            </div>
            <span className="tnum" style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 18, color: 'var(--gold-400)' }}>{PLAYER.points}</span>
          </div>
        </SystemPanel>

        {/* ===== DAILY QUEST (System window) ===== */}
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 18 }}>
          <SystemWindow
            kind="daily"
            title="Treino do Dia — Força"
            rank="C"
            goals={[
              { label: 'Flexões', current: 40, target: 100 },
              { label: 'Abdominais', current: 65, target: 100 },
              { label: 'Agachamentos', current: 30, target: 100 },
              { label: 'Corrida', current: 3.2, target: 10, unit: 'km' },
            ]}
            xp={320}
            rewards={[{ attr: 'strength', amount: 2 }, { attr: 'endurance', amount: 1 }]}
            cta="Continuar Quest"
          />
        </div>

        {/* ===== HOW QUESTS WORK ===== */}
        <SectionLabel color="var(--text-secondary)">Como funcionam as quests</SectionLabel>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 9 }}>
          {QUEST_KINDS.map((q) => (
            <div key={q.tab} style={{ display: 'flex', gap: 12, alignItems: 'flex-start', padding: '12px 14px', borderRadius: 'var(--radius-md)',
              background: 'var(--bg-surface)', border: '1px solid var(--border-default)' }}>
              <span style={{ width: 9, height: 9, marginTop: 4, flex: 'none', background: q.color, transform: 'rotate(45deg)', boxShadow: `0 0 9px color-mix(in srgb, ${q.color} 70%, transparent)` }} />
              <div>
                <div style={{ fontFamily: 'var(--font-display)', fontSize: 12, fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: q.color, marginBottom: 2 }}>{q.tab}</div>
                <p style={{ margin: 0, fontSize: 12.5, lineHeight: 1.45, color: 'var(--text-tertiary)' }}>{q.desc}</p>
              </div>
            </div>
          ))}
        </div>
        <p style={{ margin: '14px 4px 0', fontSize: 12, lineHeight: 1.5, color: 'var(--text-tertiary)' }}>
          Cada meta concluída enche a sua barra de progresso. A recompensa (XP + atributos) só é
          concedida quando <span style={{ color: 'var(--text-secondary)' }}>todas as metas</span> são
          batidas. Falhar a Quest Diária reinicia a sua ofensiva 🔥.
        </p>
      </ScreenScroll>
    </div>
  );
}

window.PlayerScreen = PlayerScreen;
