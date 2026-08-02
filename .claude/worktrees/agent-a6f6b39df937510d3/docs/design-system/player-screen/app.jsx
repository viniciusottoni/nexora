// ═══════════════════════════════════════════════════════════════
// Awaken — Player Screen App
// Screens: Home · Perfil · Inventário · Loja · Config + Quest Detail
// ═══════════════════════════════════════════════════════════════

// ─────────────────────────── DATA ───────────────────────────────

const PLAYER = {
  name: 'Vinícius Ottoni', initials: 'VO',
  level: 37, rank: 'B', className: 'STRIKER',
  xp: 648, xpMax: 900, xpToNext: 260,
  streakDays: 12, notifications: 3,
  gold: 2840, gems: 42,
  stats: { strength: 72, agility: 58, endurance: 65, vitality: 80, focus: 45, wisdom: 50 },
  onboarding: {
    goal: 'Ganhar massa muscular',
    fitnessLevel: 'Intermediário',
    trainingDays: ['Seg', 'Qua', 'Sex', 'Sáb'],
    age: 28, weight: '82 kg', height: '178 cm',
  },
};

const QUESTS = [
  {
    id: 'daily', type: 'daily', title: 'Quest Diária',
    completedCount: 2, totalCount: 4, xpReward: 120,
    exercises: [
      { id: 1, name: 'Flexão de Braço', detail: '3 séries × 20 reps', status: 'done',   attr: 'strength' },
      { id: 2, name: 'Agachamento',     detail: '4 séries × 15 reps', status: 'done',   attr: 'agility'  },
      { id: 3, name: 'Prancha',         detail: '3 séries × 60s',     status: 'active', attr: 'endurance', activeSets: 2, totalSets: 3 },
      { id: 4, name: 'Corrida',         detail: '5 km',               status: 'todo',   attr: 'vitality'  },
    ],
  },
  {
    id: 'raid', type: 'raid', title: 'Raid (nacional)',
    completedCount: 8, totalCount: 20, xpReward: 500, participants: 1247,
    exercises: [
      { id: 1, name: 'Burpees', detail: 'Meta coletiva: 10.000', current: 6800, total: 10000, unit: 'reps', attr: 'strength'  },
      { id: 2, name: 'Corrida', detail: 'Meta coletiva: 5.000',  current: 3200, total: 5000,  unit: 'km',   attr: 'agility'   },
      { id: 3, name: 'Prancha', detail: 'Meta coletiva: 500',    current: 280,  total: 500,   unit: 'min',  attr: 'endurance' },
    ],
  },
];

const INVENTORY = [
  { id: 1, name: 'Espada de Ferro',       rarity: 'common',     type: 'Arma',       icon: '⚔️', desc: 'Espada forjada em ferro puro. +5 Força durante treinos de musculação.' },
  { id: 2, name: 'Orbe de Mana',          rarity: 'uncommon',   type: 'Acessório',  icon: '🔮', desc: 'Orbe pulsante com energia azul. +8 Foco e reduz fadiga mental após o treino.' },
  { id: 3, name: 'Armadura de Couro',     rarity: 'common',     type: 'Armadura',   icon: '🛡️', desc: 'Armadura leve de couro de monstro. +4 Resistência.' },
  { id: 4, name: 'Poção de Cura',         rarity: 'consumable', type: 'Consumível', icon: '🧪', desc: 'Recuperação 20% mais rápida após treinos intensos.', qty: 3 },
  { id: 5, name: 'Fragmento de Cristal',  rarity: 'rare',       type: 'Material',   icon: '💎', desc: 'Fragmento de cristal de dungeon S. Usado para forjar itens épicos.', qty: 2 },
  { id: 6, name: 'Gema de Vitalidade',    rarity: 'rare',       type: 'Gema',       icon: '💠', desc: 'Gema de energia vital. Encanta equipamentos com +12 Vitalidade.' },
  { id: 7, name: 'Manto das Sombras',     rarity: 'epic',       type: 'Armadura',   icon: '🌑', desc: 'Manto épico das sombras. +15 Agilidade e +10 Foco.' },
  { id: 8, name: 'Pergaminho de Técnica', rarity: 'uncommon',   type: 'Pergaminho', icon: '📜', desc: 'Desbloqueia nova técnica de treino do seu estilo de combate.', qty: 1 },
  { id: 9, name: 'Escudo do Guerreiro',   rarity: 'uncommon',   type: 'Armadura',   icon: '🛡️', desc: '+6 Resistência e +3 Vitalidade. Forjado após Raid nacional.' },
];

const SHOP_ITEMS = [
  { id: 1, name: 'Poção de XP Duplo', rarity: 'uncommon', icon: '⚗️', desc: 'Dobra o XP ganho por 24 horas.',     priceGold: 350 },
  { id: 2, name: 'Título: Elite',     rarity: 'epic',     icon: '👑', desc: 'Título cosmético exclusivo.',         priceGems: 800 },
  { id: 3, name: 'Caixa de Raid',     rarity: 'rare',     icon: '📦', desc: 'Contém itens raros de dungeon.',      priceGold: 600 },
  { id: 4, name: '+10 Slots Inv.',    rarity: 'common',   icon: '🎒', desc: 'Expande o inventário em 10 slots.',   priceGems: 500 },
];

// ──────────────────── SHARED HELPERS ────────────────────────────

const CP    = 'polygon(10px 0%,100% 0%,100% calc(100% - 10px),calc(100% - 10px) 100%,0% 100%,0% 10px)';
const CP_SM = 'polygon(6px 0%,100% 0%,100% calc(100% - 6px),calc(100% - 6px) 100%,0% 100%,0% 6px)';

function Tag({ children, color = '#828AAE' }) {
  return (
    <span style={{
      fontFamily: "'Chakra Petch',sans-serif", fontSize: 10, fontWeight: 700,
      letterSpacing: '0.08em', textTransform: 'uppercase', color,
      background: `color-mix(in srgb,${color} 16%,transparent)`,
      border: `1px solid color-mix(in srgb,${color} 35%,transparent)`,
      borderRadius: 4, padding: '2px 7px',
    }}>{children}</span>
  );
}

function SectionLabel({ children, right }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
      <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 11, fontWeight: 600, letterSpacing: '0.12em', textTransform: 'uppercase', color: '#5E6488' }}>{children}</span>
      {right && <span style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 12, color: '#5E6488' }}>{right}</span>}
    </div>
  );
}

function ACard({ children, style }) {
  return (
    <div style={{
      background: '#111320', border: '1px solid rgba(255,255,255,0.08)',
      clipPath: CP,
      boxShadow: '0 6px 18px rgba(0,0,0,0.45),inset 0 1px 0 rgba(255,255,255,0.04)',
      ...(style || {}),
    }}>{children}</div>
  );
}

function AttrTag({ attr }) {
  const a = ATTR_CONFIG[attr];
  if (!a) return null;
  return <Tag color={a.color}>{a.label}</Tag>;
}

function Screen({ children }) {
  return (
    <div style={{
      flex: 1, overflowY: 'auto', WebkitOverflowScrolling: 'touch',
      padding: '20px 20px 16px',
      animation: 'fadeSlideIn 240ms cubic-bezier(0.16,1,0.3,1)',
    }}>{children}</div>
  );
}

// ──────────────────────── HOME ───────────────────────────────────

function HomeScreen({ onQuest, onProfile }) {
  return (
    <Screen>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 20 }}>
        <div onClick={onProfile} style={{
          width: 52, height: 52, borderRadius: '50%', flexShrink: 0,
          background: 'linear-gradient(135deg,#2D6FF5,#8B3FD8)',
          border: '2.5px solid #A855F7',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, fontSize: 17, color: '#fff',
          cursor: 'pointer', boxShadow: '0 0 20px rgba(139,63,216,0.45)',
        }}>VO</div>

        <div onClick={onProfile} style={{ flex: 1, cursor: 'pointer' }}>
          <div style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 10, letterSpacing: '0.14em', textTransform: 'uppercase', color: '#5E6488', marginBottom: 2 }}>BOM TREINO</div>
          <div style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 18, fontWeight: 700, color: '#F2F5FF', letterSpacing: '-0.01em', lineHeight: 1.15 }}>Vinícius Ottoni</div>
        </div>

        <div style={{ display: 'flex', gap: 8 }}>
          <div style={{ position: 'relative' }}>
            <button style={{ width: 40, height: 40, background: '#161929', border: '1px solid rgba(255,255,255,0.09)', borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center', cursor: 'pointer' }}>
              <svg width="17" height="17" viewBox="0 0 24 24" fill="#FF9500"><path d="M13.5.67s.74 2.65.74 4.8c0 2.06-1.35 3.73-3.41 3.73-2.07 0-3.63-1.67-3.63-3.73l.03-.36C5.21 7.51 4 10.62 4 14c0 4.42 3.58 8 8 8s8-3.58 8-8C20 8.61 17.41 3.8 13.5.67z"/></svg>
            </button>
            <span style={{ position: 'absolute', top: -5, right: -5, background: '#FF9500', color: '#fff', fontSize: 9, fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, borderRadius: 999, padding: '1px 5px', lineHeight: '16px', minWidth: 16, textAlign: 'center' }}>{PLAYER.streakDays}</span>
          </div>
          <div style={{ position: 'relative' }}>
            <button style={{ width: 40, height: 40, background: '#161929', border: '1px solid rgba(255,255,255,0.09)', borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center', cursor: 'pointer' }}>
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="#AEB4D0" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 01-3.46 0"/>
              </svg>
            </button>
            <span style={{ position: 'absolute', top: -5, right: -5, background: '#EF4444', color: '#fff', fontSize: 9, fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, borderRadius: 999, padding: '1px 5px', lineHeight: '16px', minWidth: 16, textAlign: 'center' }}>{PLAYER.notifications}</span>
          </div>
        </div>
      </div>

      {/* Rank Card */}
      <ACard style={{ padding: 20, marginBottom: 22 }}>
        <div style={{ display: 'flex', alignItems: 'flex-start', gap: 16, marginBottom: 16 }}>
          <RankBadge rank={PLAYER.rank} size={72} />
          <div style={{ flex: 1 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 5 }}>
              <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, fontSize: 22, color: '#F2F5FF' }}>Rank {PLAYER.rank}</span>
              <Tag color="#A855F7">{PLAYER.className}</Tag>
            </div>
            <div style={{ fontFamily: "'Sora',sans-serif", fontSize: 13, color: '#828AAE', lineHeight: 1.4 }}>
              Faltam {PLAYER.xpToNext} XP para o próximo nível
            </div>
          </div>
        </div>
        <XPBar value={PLAYER.xp} max={PLAYER.xpMax} level={PLAYER.level} height={12} />
      </ACard>

      {/* Quest List */}
      <SectionLabel right={`1/${QUESTS.length}`}>Lista de Quests</SectionLabel>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {QUESTS.map(q => (
          <button key={q.id} onClick={() => onQuest(q.id)} style={{
            display: 'flex', alignItems: 'center', justifyContent: 'space-between',
            padding: '16px 18px', background: '#111320',
            border: '1px solid rgba(255,255,255,0.08)', clipPath: CP_SM,
            cursor: 'pointer', textAlign: 'left', transition: 'background 140ms',
          }}>
            <div>
              <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontWeight: 600, fontSize: 18, color: '#F2F5FF' }}>{q.title}</span>
              <span style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 13, color: '#828AAE', marginLeft: 10 }}>{q.completedCount}/{q.totalCount}</span>
            </div>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#5E6488" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <path d="M5 12h14M12 5l7 7-7 7"/>
            </svg>
          </button>
        ))}
      </div>
    </Screen>
  );
}

// ─────────────────── QUEST DETAIL ───────────────────────────────

function DailyRow({ ex }) {
  const sc = ex.status === 'done' ? '#22C55E' : ex.status === 'active' ? '#2D6FF5' : '#474C66';
  return (
    <ACard style={{ padding: '14px 16px', opacity: ex.status === 'todo' ? 0.5 : 1 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <div style={{
          width: 28, height: 28, borderRadius: 4, flexShrink: 0,
          background: `color-mix(in srgb,${sc} 14%,transparent)`,
          border: `1.5px solid ${sc}`,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          boxShadow: ex.status === 'active' ? '0 0 10px rgba(45,111,245,0.3)' : 'none',
        }}>
          {ex.status === 'done' && (
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="#22C55E" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5"/></svg>
          )}
          {ex.status === 'active' && (
            <div style={{ width: 8, height: 8, borderRadius: '50%', background: '#2D6FF5', animation: 'pulse 1.5s ease-in-out infinite' }} />
          )}
        </div>

        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ fontFamily: "'Sora',sans-serif", fontWeight: 600, fontSize: 14, color: ex.status === 'done' ? '#5E6488' : '#F2F5FF', textDecoration: ex.status === 'done' ? 'line-through' : 'none', marginBottom: 3 }}>
            {ex.name}
          </div>
          <div style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 11, color: '#828AAE' }}>{ex.detail}</div>
          {ex.status === 'active' && ex.totalSets && (
            <div style={{ marginTop: 7 }}>
              <div style={{ display: 'flex', gap: 4, marginBottom: 3 }}>
                {Array.from({ length: ex.totalSets }).map((_, i) => (
                  <div key={i} style={{ flex: 1, height: 4, borderRadius: 999, background: i < ex.activeSets ? '#2D6FF5' : '#1D2133', boxShadow: i < ex.activeSets ? '0 0 6px rgba(45,111,245,0.5)' : 'none', transition: 'background 300ms' }} />
                ))}
              </div>
              <span style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 10, color: '#5E6488' }}>{ex.activeSets}/{ex.totalSets} séries</span>
            </div>
          )}
        </div>
        <AttrTag attr={ex.attr} />
      </div>
    </ACard>
  );
}

function RaidRow({ ex }) {
  const a = ATTR_CONFIG[ex.attr] || { color: '#828AAE' };
  const pct = Math.round((ex.current / ex.total) * 100);
  return (
    <ACard style={{ padding: '14px 16px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
        <div>
          <span style={{ fontFamily: "'Sora',sans-serif", fontWeight: 600, fontSize: 14, color: '#F2F5FF' }}>{ex.name}</span>
          <span style={{ fontFamily: "'Sora',sans-serif", fontSize: 12, color: '#5E6488', marginLeft: 8 }}>{ex.detail}</span>
        </div>
        <AttrTag attr={ex.attr} />
      </div>
      <div style={{ height: 8, borderRadius: 999, background: '#1D2133', overflow: 'hidden', marginBottom: 5 }}>
        <div style={{ height: '100%', width: `${pct}%`, borderRadius: 999, background: `linear-gradient(90deg,color-mix(in srgb,${a.color} 70%,#000),${a.color})`, boxShadow: `0 0 8px color-mix(in srgb,${a.color} 55%,transparent)`, transition: 'width 600ms cubic-bezier(0.16,1,0.3,1)' }} />
      </div>
      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
        <span style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 11, color: '#828AAE' }}>{ex.current.toLocaleString('pt-BR')} / {ex.total.toLocaleString('pt-BR')} {ex.unit}</span>
        <span style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 11, fontWeight: 700, color: a.color }}>{pct}%</span>
      </div>
    </ACard>
  );
}

function QuestDetailScreen({ quest, onBack }) {
  if (!quest) return null;
  const isRaid = quest.type === 'raid';
  const progress = (quest.completedCount / quest.totalCount) * 100;
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', animation: 'fadeSlideIn 240ms cubic-bezier(0.16,1,0.3,1)' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '14px 20px', borderBottom: '1px solid rgba(255,255,255,0.07)', background: '#0A0B12', flexShrink: 0 }}>
        <button onClick={onBack} style={{ width: 36, height: 36, background: '#161929', border: '1px solid rgba(255,255,255,0.09)', borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', cursor: 'pointer', flexShrink: 0 }}>
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#AEB4D0" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
            <path d="M19 12H5M12 19l-7-7 7-7"/>
          </svg>
        </button>
        <div style={{ flex: 1 }}>
          <div style={{ fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, fontSize: 16, color: '#F2F5FF' }}>{quest.title}</div>
          {isRaid && <div style={{ fontFamily: "'Sora',sans-serif", fontSize: 11, color: '#5E6488', marginTop: 1 }}>{quest.participants?.toLocaleString('pt-BR')} participantes ativos</div>}
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, fontSize: 12, letterSpacing: '0.08em', color: '#FFD64A' }}>+{quest.xpReward} XP</div>
          <div style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 11, color: '#5E6488', marginTop: 1 }}>{quest.completedCount}/{quest.totalCount}</div>
        </div>
      </div>

      <div style={{ padding: '10px 20px 0', flexShrink: 0 }}>
        <div style={{ height: 4, borderRadius: 999, background: '#1D2133', overflow: 'hidden' }}>
          <div style={{ height: '100%', width: `${progress}%`, borderRadius: 999, background: 'linear-gradient(90deg,#2D6FF5,#8B3FD8)', transition: 'width 600ms cubic-bezier(0.16,1,0.3,1)' }} />
        </div>
      </div>

      <Screen>
        <SectionLabel>{isRaid ? 'Progresso Coletivo' : 'Exercícios'}</SectionLabel>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {quest.exercises.map(ex => isRaid ? <RaidRow key={ex.id} ex={ex} /> : <DailyRow key={ex.id} ex={ex} />)}
        </div>
      </Screen>
    </div>
  );
}

// ─────────────────────── PERFIL ─────────────────────────────────

function ProfileScreen() {
  const ob = PLAYER.onboarding;
  const fields = [
    { label: 'Meta', value: ob.goal },
    { label: 'Nível', value: ob.fitnessLevel },
    { label: 'Dias de Treino', value: ob.trainingDays.join(' · ') },
    { label: 'Idade', value: `${ob.age} anos` },
    { label: 'Peso', value: ob.weight },
    { label: 'Altura', value: ob.height },
  ];
  return (
    <Screen>
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginBottom: 24 }}>
        <div style={{
          width: 80, height: 80, borderRadius: '50%', marginBottom: 14,
          background: 'linear-gradient(135deg,#2D6FF5,#8B3FD8)',
          border: '3px solid #A855F7',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, fontSize: 26, color: '#fff',
          boxShadow: '0 0 28px rgba(139,63,216,0.5)',
        }}>VO</div>
        <div style={{ fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, fontSize: 20, color: '#F2F5FF', marginBottom: 8 }}>{PLAYER.name}</div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <RankBadge rank={PLAYER.rank} size={30} glow={false} />
          <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 13, color: '#AEB4D0' }}>Rank {PLAYER.rank}</span>
          <Tag color="#A855F7">{PLAYER.className}</Tag>
        </div>
      </div>

      <SectionLabel>Atributos</SectionLabel>
      <ACard style={{ padding: '16px 18px', marginBottom: 20 }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          {Object.entries(PLAYER.stats).map(([attr, value]) => (
            <StatBar key={attr} attr={attr} value={value} max={100} />
          ))}
        </div>
      </ACard>

      <SectionLabel>Configuração de Treino</SectionLabel>
      <ACard style={{ padding: '4px 18px', marginBottom: 20 }}>
        {fields.map((f, i) => (
          <div key={f.label} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '11px 0', borderBottom: i < fields.length - 1 ? '1px solid rgba(255,255,255,0.05)' : 'none' }}>
            <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 11, fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#5E6488' }}>{f.label}</span>
            <span style={{ fontFamily: "'Sora',sans-serif", fontSize: 13, color: '#AEB4D0', textAlign: 'right', maxWidth: '58%' }}>{f.value}</span>
          </div>
        ))}
      </ACard>

      <button style={{ width: '100%', padding: 14, background: 'rgba(45,111,245,0.1)', border: '1px solid rgba(45,111,245,0.3)', clipPath: CP, cursor: 'pointer', fontFamily: "'Chakra Petch',sans-serif", fontWeight: 600, fontSize: 13, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#4D8BFF' }}>
        Editar Perfil
      </button>
    </Screen>
  );
}

// ─────────────────── INVENTÁRIO ─────────────────────────────────

function InventoryScreen({ onItemSelect }) {
  const [filter, setFilter] = React.useState('all');
  const chips = ['all', 'common', 'uncommon', 'rare', 'epic', 'consumable'];
  const filtered = filter === 'all' ? INVENTORY : INVENTORY.filter(i => i.rarity === filter);

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', animation: 'fadeSlideIn 240ms cubic-bezier(0.16,1,0.3,1)' }}>
      <div style={{ padding: '20px 20px 0', flexShrink: 0 }}>
        <SectionLabel right={`${INVENTORY.length} itens`}>Inventário</SectionLabel>
        <div style={{ display: 'flex', gap: 6, overflowX: 'auto', paddingBottom: 12, scrollbarWidth: 'none' }}>
          {chips.map(f => {
            const rc = RARITY_CONFIG[f];
            const active = filter === f;
            const c = rc ? rc.color : '#AEB4D0';
            return (
              <button key={f} onClick={() => setFilter(f)} style={{
                flexShrink: 0, padding: '4px 12px',
                background: active ? `color-mix(in srgb,${c} 18%,transparent)` : 'transparent',
                border: `1px solid ${active ? c : 'rgba(255,255,255,0.1)'}`,
                borderRadius: 999, cursor: 'pointer',
                fontFamily: "'Chakra Petch',sans-serif", fontSize: 10, fontWeight: 600,
                letterSpacing: '0.08em', textTransform: 'uppercase',
                color: active ? c : '#5E6488', transition: 'all 140ms',
              }}>{f === 'all' ? 'Todos' : rc.label}</button>
            );
          })}
        </div>
      </div>

      <div style={{ flex: 1, overflowY: 'auto', WebkitOverflowScrolling: 'touch', padding: '0 20px 16px' }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 8 }}>
          {filtered.map(item => {
            const rc = RARITY_CONFIG[item.rarity] || { color: '#333A55', label: '?' };
            return (
              <button key={item.id} onClick={() => onItemSelect(item)} style={{
                background: '#111320',
                border: `1px solid color-mix(in srgb,${rc.color} 28%,rgba(255,255,255,0.05))`,
                borderRadius: 8, padding: '12px 8px',
                cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 7,
                transition: 'all 140ms', position: 'relative',
              }}>
                <div style={{ fontSize: 26, lineHeight: 1 }}>{item.icon}</div>
                <div style={{ fontFamily: "'Sora',sans-serif", fontSize: 10, fontWeight: 600, color: '#C3C9E6', textAlign: 'center', lineHeight: 1.35 }}>{item.name}</div>
                <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 9, fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase', color: rc.color }}>{rc.label}</span>
                {item.qty != null && <span style={{ position: 'absolute', top: 5, right: 7, fontFamily: "'JetBrains Mono',monospace", fontSize: 10, color: '#5E6488' }}>×{item.qty}</span>}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// ──────────────────────── LOJA ───────────────────────────────────

function ShopScreen() {
  const balances = [
    { icon: '🪙', label: 'OURO',  value: PLAYER.gold.toLocaleString('pt-BR'), color: '#FFD64A' },
    { icon: '💎', label: 'GEMAS', value: PLAYER.gems, color: '#5FE8FF' },
  ];
  return (
    <Screen>
      <div style={{ display: 'flex', gap: 10, marginBottom: 20 }}>
        {balances.map(b => (
          <ACard key={b.label} style={{ flex: 1, padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 10 }}>
            <span style={{ fontSize: 22 }}>{b.icon}</span>
            <div>
              <div style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 10, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#5E6488' }}>{b.label}</div>
              <div style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 18, fontWeight: 700, color: b.color }}>{b.value}</div>
            </div>
          </ACard>
        ))}
      </div>

      <SectionLabel>Destaque</SectionLabel>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {SHOP_ITEMS.map(item => {
          const rc = RARITY_CONFIG[item.rarity] || {};
          const price = item.priceGold != null
            ? { icon: '🪙', value: item.priceGold.toLocaleString('pt-BR'), color: '#FFD64A' }
            : { icon: '💎', value: item.priceGems, color: '#5FE8FF' };
          return (
            <ACard key={item.id} style={{ padding: '14px 16px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                <div style={{ width: 48, height: 48, background: '#161929', border: `1px solid color-mix(in srgb,${rc.color || '#333'} 28%,transparent)`, borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 24, flexShrink: 0 }}>{item.icon}</div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontFamily: "'Sora',sans-serif", fontWeight: 600, fontSize: 14, color: '#F2F5FF', marginBottom: 3 }}>{item.name}</div>
                  <div style={{ fontFamily: "'Sora',sans-serif", fontSize: 12, color: '#5E6488' }}>{item.desc}</div>
                </div>
                <button style={{ flexShrink: 0, padding: '8px 12px', background: `color-mix(in srgb,${price.color} 14%,transparent)`, border: `1px solid color-mix(in srgb,${price.color} 40%,transparent)`, borderRadius: 6, cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
                  <span style={{ fontSize: 14 }}>{price.icon}</span>
                  <span style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 11, fontWeight: 700, color: price.color }}>{price.value}</span>
                </button>
              </div>
            </ACard>
          );
        })}
      </div>
    </Screen>
  );
}

// ─────────────────── CONFIGURAÇÕES ──────────────────────────────

function SettingsScreen() {
  const [musicVol, setMusicVol] = React.useState(75);
  const [fxVol, setFxVol]       = React.useState(100);
  const [lang, setLang]         = React.useState('pt');
  const langs    = [{ id: 'pt', label: 'Português' }, { id: 'en', label: 'English' }, { id: 'es', label: 'Español' }];
  const support  = [{ icon: '💬', label: 'Fale Conosco' }, { icon: '❓', label: 'FAQ' }, { icon: 'ℹ️', label: 'Sobre o Awaken' }];
  const sliders  = [{ label: 'Música', val: musicVol, set: setMusicVol }, { label: 'Efeitos', val: fxVol, set: setFxVol }];

  return (
    <Screen>
      <SectionLabel>Som</SectionLabel>
      <ACard style={{ padding: '4px 18px', marginBottom: 20 }}>
        {sliders.map((s, i) => (
          <div key={s.label} style={{ padding: '12px 0', borderBottom: i < sliders.length - 1 ? '1px solid rgba(255,255,255,0.05)' : 'none' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 10 }}>
              <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 12, fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: '#AEB4D0' }}>{s.label}</span>
              <span style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 12, color: '#5E6488' }}>{s.val}%</span>
            </div>
            <input type="range" min="0" max="100" value={s.val} onChange={e => s.set(Number(e.target.value))} style={{ width: '100%', cursor: 'pointer', accentColor: '#2D6FF5' }} />
          </div>
        ))}
      </ACard>

      <SectionLabel>Idioma</SectionLabel>
      <ACard style={{ padding: 6, marginBottom: 20 }}>
        <div style={{ display: 'flex', gap: 4 }}>
          {langs.map(l => (
            <button key={l.id} onClick={() => setLang(l.id)} style={{
              flex: 1, padding: '10px 6px',
              background: lang === l.id ? 'rgba(45,111,245,0.18)' : 'transparent',
              border: `1px solid ${lang === l.id ? 'rgba(77,139,255,0.5)' : 'transparent'}`,
              borderRadius: 6, cursor: 'pointer',
              fontFamily: "'Chakra Petch',sans-serif", fontSize: 11, fontWeight: 600,
              letterSpacing: '0.03em', color: lang === l.id ? '#4D8BFF' : '#5E6488', transition: 'all 140ms',
            }}>{l.label}</button>
          ))}
        </div>
      </ACard>

      <SectionLabel>Suporte</SectionLabel>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {support.map(item => (
          <ACard key={item.label} style={{ padding: '14px 16px', cursor: 'pointer' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <span style={{ fontSize: 18 }}>{item.icon}</span>
                <span style={{ fontFamily: "'Sora',sans-serif", fontSize: 14, color: '#AEB4D0' }}>{item.label}</span>
              </div>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#5E6488" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><path d="M9 18l6-6-6-6"/></svg>
            </div>
          </ACard>
        ))}
      </div>
    </Screen>
  );
}

// ─────────────────── ITEM MODAL ─────────────────────────────────

function ItemModal({ item, onClose }) {
  const rc = RARITY_CONFIG[item.rarity] || { color: '#828AAE', label: '?' };
  return (
    <div onClick={onClose} style={{ position: 'absolute', inset: 0, background: 'rgba(7,8,13,0.84)', backdropFilter: 'blur(8px)', display: 'flex', alignItems: 'flex-end', zIndex: 100 }}>
      <div onClick={e => e.stopPropagation()} style={{
        width: '100%', background: '#111320',
        border: '1px solid rgba(255,255,255,0.1)', borderBottom: 'none',
        borderRadius: '16px 16px 0 0', padding: '20px 20px 48px',
        boxShadow: '0 -14px 40px rgba(0,0,0,0.6)',
        animation: 'slideUp 260ms cubic-bezier(0.16,1,0.3,1)',
      }}>
        <div style={{ width: 36, height: 3, borderRadius: 999, background: 'rgba(255,255,255,0.15)', margin: '0 auto 20px' }} />

        <div style={{ display: 'flex', gap: 16, alignItems: 'flex-start', marginBottom: 16 }}>
          <div style={{
            width: 64, height: 64, background: '#161929',
            border: `1.5px solid color-mix(in srgb,${rc.color} 40%,transparent)`,
            borderRadius: 10, display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 30, flexShrink: 0,
            boxShadow: `0 0 22px color-mix(in srgb,${rc.color} 28%,transparent)`,
          }}>{item.icon}</div>
          <div>
            <div style={{ fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, fontSize: 17, color: '#F2F5FF', marginBottom: 8 }}>{item.name}</div>
            <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
              <Tag color={rc.color}>{rc.label}</Tag>
              <Tag color="#828AAE">{item.type}</Tag>
            </div>
          </div>
        </div>

        <p style={{ fontFamily: "'Sora',sans-serif", fontSize: 14, color: '#AEB4D0', lineHeight: 1.65, margin: '0 0 20px' }}>{item.desc}</p>
        {item.qty != null && <div style={{ fontFamily: "'JetBrains Mono',monospace", fontSize: 12, color: '#5E6488', marginBottom: 16 }}>Quantidade: ×{item.qty}</div>}

        <button style={{
          width: '100%', padding: 14,
          background: `linear-gradient(135deg,color-mix(in srgb,${rc.color} 18%,rgba(45,111,245,0.1)),color-mix(in srgb,${rc.color} 10%,rgba(139,63,216,0.08)))`,
          border: `1px solid color-mix(in srgb,${rc.color} 50%,rgba(77,139,255,0.15))`,
          clipPath: CP_SM, cursor: 'pointer',
          fontFamily: "'Chakra Petch',sans-serif", fontWeight: 700, fontSize: 13,
          letterSpacing: '0.1em', textTransform: 'uppercase', color: rc.color,
        }}>{item.rarity === 'consumable' ? 'Usar Item' : 'Equipar'}</button>
      </div>
    </div>
  );
}

// ─────────────────── BOTTOM NAV ─────────────────────────────────

const NAV_TABS = [
  { id: 'home', label: 'HOME', icon:
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/>
    </svg> },
  { id: 'profile', label: 'PERFIL', icon:
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="12" cy="7" r="4"/>
    </svg> },
  { id: 'inventory', label: 'INVENTÁRIO', icon:
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 7V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v2"/>
    </svg> },
  { id: 'shop', label: 'LOJA', icon:
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 01-8 0"/>
    </svg> },
  { id: 'settings', label: 'CONFIG', icon:
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="3"/>
      <path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/>
    </svg> },
];

function BottomNav({ tab, onTab }) {
  return (
    <div style={{ flexShrink: 0, display: 'flex', borderTop: '1px solid rgba(255,255,255,0.07)', background: '#080A10' }}>
      {NAV_TABS.map(t => {
        const active = tab === t.id;
        return (
          <button key={t.id} onClick={() => onTab(t.id)} style={{
            flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
            gap: 3, padding: '9px 2px', background: 'transparent', border: 'none', cursor: 'pointer',
            color: active ? '#2D6FF5' : '#5E6488', transition: 'color 140ms', minHeight: 54, position: 'relative',
          }}>
            {active && (
              <div style={{ position: 'absolute', top: 0, left: '50%', transform: 'translateX(-50%)', width: 24, height: 2, background: '#2D6FF5', borderRadius: '0 0 2px 2px', boxShadow: '0 0 8px #2D6FF5' }} />
            )}
            {t.icon}
            <span style={{ fontFamily: "'Chakra Petch',sans-serif", fontSize: 8, fontWeight: 700, letterSpacing: '0.07em', textTransform: 'uppercase', lineHeight: 1 }}>{t.label}</span>
          </button>
        );
      })}
    </div>
  );
}

// ──────────────────────── APP ────────────────────────────────────

function App() {
  const [tab, setTab]               = React.useState('home');
  const [questId, setQuestId]       = React.useState(null);
  const [selectedItem, setSelectedItem] = React.useState(null);

  function goTab(t) { setTab(t); setQuestId(null); }

  const quest = questId ? QUESTS.find(q => q.id === questId) : null;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', width: '100%', height: '100%', background: '#0A0B12', position: 'relative', overflow: 'hidden' }}>
      {/* Ambient glow */}
      <div style={{ position: 'absolute', top: -100, left: '50%', transform: 'translateX(-50%)', width: 340, height: 340, borderRadius: '50%', background: 'radial-gradient(circle,rgba(45,111,245,0.07) 0%,transparent 70%)', pointerEvents: 'none', zIndex: 0 }} />

      {quest ? (
        <QuestDetailScreen quest={quest} onBack={() => setQuestId(null)} />
      ) : (
        <>
          <div key={tab} style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', position: 'relative', zIndex: 1 }}>
            {tab === 'home'      && <HomeScreen onQuest={setQuestId} onProfile={() => goTab('profile')} />}
            {tab === 'profile'   && <ProfileScreen />}
            {tab === 'inventory' && <InventoryScreen onItemSelect={setSelectedItem} />}
            {tab === 'shop'      && <ShopScreen />}
            {tab === 'settings'  && <SettingsScreen />}
          </div>
          <BottomNav tab={tab} onTab={goTab} />
        </>
      )}

      {selectedItem && <ItemModal item={selectedItem} onClose={() => setSelectedItem(null)} />}
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
