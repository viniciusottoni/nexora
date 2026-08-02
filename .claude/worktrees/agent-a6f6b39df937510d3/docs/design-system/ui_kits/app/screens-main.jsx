/* Awaken UI kit — main app: Home (daily quests), Profile (hunter card), Workout. */
const Icon = window.AwakenIcon;
const { Button, Badge, Card, Avatar, RankBadge, XPBar, StatBar, QuestCard, ProgressRing } = window.AwakenDesignSystem_956798;
const { StatusBar, ScreenScroll, AppHeader, IconBtn, TabBar } = window;

const HUNTER = {
  name: 'Kael Voss', rank: 'B', level: 37, klass: 'Striker', streak: 12,
  xp: 640, xpMax: 900,
  attrs: { strength: 72, agility: 54, endurance: 61, vitality: 68, focus: 45, wisdom: 58 },
};

const QUEST_ICON = { strength: 'dumbbell', agility: 'wind', endurance: 'activity', vitality: 'shield', focus: 'target', wisdom: 'eye' };
function todayQuests() {
  return [
    { id: 1, title: 'Flexões — 4 × 12', subtitle: 'Peito · sem equipamento', xp: 120, attr: 'strength' },
    { id: 2, title: 'Agachamento livre — 4 × 15', subtitle: 'Pernas · sem equipamento', xp: 140, attr: 'endurance' },
    { id: 3, title: 'Prancha — 3 × 45s', subtitle: 'Core · foco', xp: 80, attr: 'focus' },
    { id: 4, title: 'Burpees — 3 × 10', subtitle: 'Full body · cardio', xp: 110, attr: 'agility' },
  ];
}

/* ---------------- Home ---------------- */
function Home({ go, quests, toggleQuest }) {
  const doneCount = quests.filter((q) => q.done).length;
  const earned = quests.filter((q) => q.done).reduce((s, q) => s + q.xp, 0);
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: 'var(--grad-void)' }}>
      <StatusBar />
      <AppHeader
        eyebrow="Bom treino, hunter"
        title="Kael"
        left={<Avatar name={HUNTER.name} rank={HUNTER.rank} size={44} onClick={() => go('profile')} style={{ cursor: 'pointer' }} />}
        right={<div style={{ display: 'flex', gap: 8 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 5, padding: '0 11px', height: 40, borderRadius: 'var(--radius-md)', background: 'var(--bg-surface)', border: '1px solid var(--border-default)' }}>
            <Icon name="flame" size={17} color="var(--orange-500)" />
            <span className="tnum" style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 15, color: 'var(--text-primary)' }}>{HUNTER.streak}</span>
          </div>
          <IconBtn name="bell" badge />
        </div>}
      />
      <ScreenScroll style={{ padding: '0 20px 24px' }}>
        {/* Rank + XP hero */}
        <Card variant="energy" padding={18} style={{ marginBottom: 16 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 16 }}>
            <RankBadge rank={HUNTER.rank} size={66} />
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 19, color: 'var(--text-primary)' }}>Rank {HUNTER.rank}</span>
                <Badge tone="purple" variant="soft">{HUNTER.klass}</Badge>
              </div>
              <div style={{ fontSize: 13, color: 'var(--text-tertiary)', marginTop: 3 }}>Faltam {HUNTER.xpMax - HUNTER.xp} XP para o próximo nível</div>
            </div>
          </div>
          <XPBar level={HUNTER.level} value={HUNTER.xp} max={HUNTER.xpMax} />
        </Card>

        {/* Daily quest header */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', margin: '4px 2px 12px' }}>
          <div className="eyebrow" style={{ color: 'var(--text-secondary)' }}>Quest diária · hoje</div>
          <span className="tnum" style={{ fontFamily: 'var(--font-mono)', fontSize: 12, color: 'var(--text-tertiary)' }}>{doneCount}/{quests.length} · +{earned} XP</span>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 20 }}>
          {quests.map((q, i) => (
            <QuestCard key={q.id} title={q.title} subtitle={q.subtitle} xp={q.xp} attr={q.attr}
              status={q.done ? 'done' : i === doneCount ? 'active' : 'todo'}
              icon={<Icon name={QUEST_ICON[q.attr]} size={20} />}
              onToggle={() => toggleQuest(q.id)} onClick={() => go('workout')} />
          ))}
        </div>

        <Button variant="primary" size="lg" glow fullWidth onClick={() => go('workout')}
          leftIcon={<Icon name="play" size={17} />}>Iniciar treino completo</Button>

        {/* Nutrition */}
        <div className="eyebrow" style={{ color: 'var(--text-secondary)', margin: '24px 2px 12px' }}>Status do dia</div>
        <div style={{ display: 'flex', gap: 12 }}>
          {[
            { ring: <ProgressRing value={6} max={8} size={84} color="var(--info)" label="6" sublabel="/ 8 copos" />, icon: 'droplet', label: 'Água' },
            { ring: <ProgressRing value={88} max={140} size={84} color="var(--attr-strength)" label="88" sublabel="/ 140 g" />, icon: 'zap', label: 'Proteína' },
            { ring: <ProgressRing value={1820} max={2200} size={84} color="var(--attr-vitality)" label="1.8k" sublabel="kcal" />, icon: 'flame', label: 'Calorias' },
          ].map((t) => (
            <Card key={t.label} padding={12} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8 }}>
              {t.ring}
              <span style={{ fontSize: 12, color: 'var(--text-tertiary)' }}>{t.label}</span>
            </Card>
          ))}
        </div>
      </ScreenScroll>
    </div>
  );
}

/* ---------------- Profile (Hunter card) ---------------- */
const ATTR_META = [
  { key: 'strength', label: 'Força', icon: 'dumbbell' },
  { key: 'agility', label: 'Agilidade', icon: 'wind' },
  { key: 'endurance', label: 'Resistência', icon: 'activity' },
  { key: 'vitality', label: 'Vitalidade', icon: 'shield' },
  { key: 'focus', label: 'Foco', icon: 'target' },
  { key: 'wisdom', label: 'Sabedoria', icon: 'eye' },
];

function Profile() {
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: 'var(--grad-void)' }}>
      <StatusBar />
      <AppHeader eyebrow="Hunter" title="Perfil" right={<IconBtn name="settings" />} />
      <ScreenScroll style={{ padding: '0 20px 24px' }}>
        {/* Shareable hunter card */}
        <div style={{ position: 'relative', borderRadius: 22, overflow: 'hidden', padding: 22,
          background: 'radial-gradient(130% 100% at 50% 0%, #1a2348, #0b0e1c 70%)',
          border: '1px solid color-mix(in srgb, var(--rank-b) 45%, transparent)',
          boxShadow: '0 0 34px color-mix(in srgb, var(--rank-b) 26%, transparent)', marginBottom: 18 }}>
          <div style={{ position: 'absolute', top: -30, right: -30, width: 160, height: 160, background: 'radial-gradient(circle, rgba(139,63,216,0.4), transparent 65%)' }} />
          <div style={{ position: 'relative', display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 18 }}>
            <div className="eyebrow" style={{ color: 'var(--blue-200)' }}>Hunter Card</div>
            <IconBtn name="share" />
          </div>
          <div style={{ position: 'relative', display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center' }}>
            <Avatar name={HUNTER.name} rank={HUNTER.rank} size={92} />
            <h2 style={{ fontSize: 24, fontWeight: 700, color: '#fff', marginTop: 14 }}>{HUNTER.name}</h2>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 8 }}>
              <RankBadge rank={HUNTER.rank} size={30} glow={false} />
              <span style={{ fontFamily: 'var(--font-display)', fontWeight: 600, fontSize: 14, color: 'var(--text-secondary)', letterSpacing: '0.05em' }}>RANK {HUNTER.rank} · NÍVEL {HUNTER.level}</span>
            </div>
            <div style={{ display: 'flex', gap: 8, marginTop: 14 }}>
              <Badge tone="purple" variant="soft">{HUNTER.klass}</Badge>
              <Badge tone="red" variant="soft" icon={<Icon name="flame" size={12} />}>Streak {HUNTER.streak}</Badge>
            </div>
          </div>
        </div>

        {/* Attributes */}
        <div className="eyebrow" style={{ color: 'var(--text-secondary)', margin: '6px 2px 14px' }}>Atributos</div>
        <Card padding={18} style={{ marginBottom: 18 }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 15 }}>
            {ATTR_META.map((a) => <StatBar key={a.key} attr={a.key} value={HUNTER.attrs[a.key]} />)}
          </div>
        </Card>

        {/* Achievements */}
        <div className="eyebrow" style={{ color: 'var(--text-secondary)', margin: '6px 2px 14px' }}>Conquistas</div>
        <div style={{ display: 'flex', gap: 10 }}>
          {[
            { icon: 'flame', tone: 'var(--orange-500)', label: 'Streak 7' },
            { icon: 'trophy', tone: 'var(--gold-400)', label: 'Rank up' },
            { icon: 'zap', tone: 'var(--blue-300)', label: '10k XP' },
            { icon: 'lock', tone: 'var(--text-disabled)', label: 'Master', locked: true },
          ].map((b) => (
            <Card key={b.label} padding={12} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 9, opacity: b.locked ? 0.5 : 1 }}>
              <div style={{ width: 46, height: 46, borderRadius: '50%', display: 'grid', placeItems: 'center', background: 'var(--bg-elevated)', border: `1px solid ${b.locked ? 'var(--border-default)' : 'color-mix(in srgb,' + b.tone + ' 45%, transparent)'}`, color: b.tone, boxShadow: b.locked ? 'none' : `0 0 14px color-mix(in srgb, ${b.tone} 30%, transparent)` }}>
                <Icon name={b.icon} size={20} />
              </div>
              <span style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>{b.label}</span>
            </Card>
          ))}
        </div>
      </ScreenScroll>
    </div>
  );
}

/* ---------------- Workout (live quest) ---------------- */
function Workout({ go, quests, completeWorkout }) {
  const [set, setSet] = React.useState(1);
  const totalSets = 4;
  const ex = { name: 'Flexões', target: '12 repetições', attr: 'strength', xp: 120, muscle: 'Peito · Tríceps' };
  const nextSet = () => { if (set < totalSets) setSet(set + 1); else completeWorkout(); };

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: 'var(--grad-void)' }}>
      <StatusBar />
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '4px 20px 10px' }}>
        <button onClick={() => go('home')} style={{ width: 40, height: 40, borderRadius: 'var(--radius-md)', background: 'var(--bg-surface)', border: '1px solid var(--border-default)', display: 'grid', placeItems: 'center', color: 'var(--text-secondary)', cursor: 'pointer' }}>
          <Icon name="chevronLeft" size={18} />
        </button>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontFamily: 'var(--font-mono)', fontSize: 14, color: 'var(--text-secondary)' }}>
          <Icon name="timer" size={16} color="var(--blue-300)" /><span className="tnum">12:48</span>
        </div>
        <button onClick={() => go('home')} style={{ width: 40, height: 40, borderRadius: 'var(--radius-md)', background: 'var(--bg-surface)', border: '1px solid var(--border-default)', display: 'grid', placeItems: 'center', color: 'var(--text-secondary)', cursor: 'pointer' }}>
          <Icon name="x" size={18} />
        </button>
      </div>

      <ScreenScroll style={{ padding: '8px 22px 0', display: 'flex', flexDirection: 'column' }}>
        <div className="eyebrow" style={{ color: 'var(--blue-300)' }}>Quest em andamento · 1 de {quests.length}</div>
        <h1 style={{ fontSize: 34, fontWeight: 700, color: 'var(--text-primary)', margin: '6px 0 4px', textTransform: 'uppercase' }}>{ex.name}</h1>
        <div style={{ display: 'flex', gap: 8, marginBottom: 22 }}>
          <Badge tone="red" variant="soft">Força</Badge>
          <Badge tone="neutral" variant="outline">{ex.muscle}</Badge>
        </div>

        {/* Big set ring */}
        <div style={{ display: 'flex', justifyContent: 'center', margin: '8px 0 24px' }}>
          <ProgressRing value={set} max={totalSets} size={200} stroke={14} color="energy">
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontFamily: 'var(--font-mono)', fontSize: 11, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--text-tertiary)' }}>Série</div>
              <div className="tnum" style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 56, color: 'var(--text-primary)', lineHeight: 1 }}>{set}<span style={{ fontSize: 26, color: 'var(--text-tertiary)' }}>/{totalSets}</span></div>
              <div style={{ fontFamily: 'var(--font-display)', fontSize: 16, fontWeight: 600, color: 'var(--blue-200)', marginTop: 4, textTransform: 'uppercase' }}>{ex.target}</div>
            </div>
          </ProgressRing>
        </div>

        {/* set pips */}
        <div style={{ display: 'flex', gap: 8, justifyContent: 'center', marginBottom: 26 }}>
          {Array.from({ length: totalSets }).map((_, i) => (
            <div key={i} style={{ width: 44, height: 6, borderRadius: 999, background: i < set ? 'var(--grad-energy)' : 'var(--ink-700)', boxShadow: i < set ? 'var(--glow-blue-sm)' : 'none' }} />
          ))}
        </div>

        <Card padding={14} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 18 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <Icon name="wind" size={18} color="var(--text-tertiary)" />
            <span style={{ fontSize: 14, color: 'var(--text-secondary)' }}>Muito difícil? Trocar variante</span>
          </div>
          <Icon name="chevronRight" size={18} color="var(--text-tertiary)" />
        </Card>
      </ScreenScroll>

      <div style={{ padding: '8px 22px 34px' }}>
        <Button variant="primary" size="lg" glow fullWidth onClick={nextSet}
          rightIcon={<Icon name={set < totalSets ? 'check' : 'zap'} size={18} />}>
          {set < totalSets ? 'Série concluída' : `Finalizar · +${ex.xp} XP`}
        </Button>
      </div>
    </div>
  );
}

/* ---------------- Level-up overlay ---------------- */
function LevelUp({ onClose }) {
  return (
    <div onClick={onClose} style={{ position: 'absolute', inset: 0, zIndex: 60, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
      background: 'radial-gradient(circle at 50% 40%, rgba(45,111,245,0.35), rgba(7,8,13,0.92) 60%)', backdropFilter: 'blur(4px)', padding: 32, textAlign: 'center', cursor: 'pointer' }}>
      <div className="eyebrow" style={{ color: 'var(--gold-400)', fontSize: 13 }}>Quest completa</div>
      <div style={{ position: 'relative', margin: '18px 0' }}>
        <div style={{ position: 'absolute', inset: -30, background: 'radial-gradient(circle, rgba(139,63,216,0.5), transparent 65%)' }} />
        <RankBadge rank="B" size={130} />
      </div>
      <h1 style={{ fontSize: 30, fontWeight: 700, color: '#fff', textTransform: 'uppercase', lineHeight: 1.1 }}>Você subiu para<br/>o nível 38</h1>
      <div style={{ display: 'flex', gap: 10, marginTop: 18 }}>
        <Badge tone="gold" variant="solid">+450 XP</Badge>
        <Badge tone="red" variant="soft" icon={<Icon name="dumbbell" size={12} />}>Força +1</Badge>
      </div>
      <p style={{ fontSize: 13, color: 'var(--text-tertiary)', marginTop: 26 }}>Toque para continuar</p>
    </div>
  );
}

Object.assign(window, { Home, Profile, Workout, LevelUp, todayQuests });
