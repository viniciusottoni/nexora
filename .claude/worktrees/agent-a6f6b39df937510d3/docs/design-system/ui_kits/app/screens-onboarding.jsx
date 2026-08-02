/* Awaken UI kit — entry flow: Splash → Plans (up-front) → Onboarding. */
const Icon = window.AwakenIcon;
const { Button, Badge, Chip, Card } = window.AwakenDesignSystem_956798;
const { StatusBar } = window;

/* ---------- Splash ---------- */
function Splash({ onStart }) {
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: 'var(--grad-void)' }}>
      <StatusBar />
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '0 32px', textAlign: 'center' }}>
        <div style={{ position: 'relative', marginBottom: 30 }}>
          <div style={{ position: 'absolute', inset: -40, background: 'radial-gradient(circle, rgba(45,111,245,0.35), transparent 65%)', filter: 'blur(8px)' }} />
          <img src="../../assets/logo-mark.png" alt="Awaken" style={{ width: 200, position: 'relative', display: 'block' }} />
        </div>
        <img src="../../assets/logo-wordmark.png" alt="AWAKEN" style={{ width: 230, marginBottom: 22 }} />
        <p style={{ fontFamily: 'var(--font-display)', fontSize: 17, fontWeight: 500, letterSpacing: '0.04em', color: 'var(--blue-200)', margin: 0, textTransform: 'uppercase' }}>
          Desperte o seu potencial
        </p>
        <p style={{ fontSize: 14.5, lineHeight: 1.6, color: 'var(--text-tertiary)', maxWidth: 290, marginTop: 14 }}>
          A academia é o dungeon. Você é o hunter. Cada treino é uma quest.
        </p>
      </div>
      <div style={{ padding: '0 24px 40px', display: 'flex', flexDirection: 'column', gap: 12 }}>
        <Button variant="primary" size="lg" glow fullWidth onClick={onStart}
          rightIcon={<Icon name="arrowRight" size={18} />}>Começar</Button>
        <button onClick={onStart} style={{ background: 'none', border: 'none', color: 'var(--text-tertiary)', fontSize: 14, cursor: 'pointer', fontFamily: 'var(--font-body)' }}>
          Já tenho conta · <span style={{ color: 'var(--blue-300)' }}>Entrar</span>
        </button>
      </div>
    </div>
  );
}

/* ---------- Plans (shown BEFORE onboarding — honest freemium) ---------- */
function PlanRow({ icon, children, on = true }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 13.5, color: on ? 'var(--text-secondary)' : 'var(--text-disabled)' }}>
      <Icon name={on ? 'check' : 'x'} size={15} color={on ? 'var(--success)' : 'var(--text-disabled)'} strokeWidth={3} />
      {children}
    </div>
  );
}

function Plans({ onContinue }) {
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: 'var(--grad-void)' }}>
      <StatusBar />
      <div style={{ flex: 1, overflowY: 'auto', padding: '6px 22px 0' }}>
        <div className="eyebrow" style={{ color: 'var(--blue-300)' }}>Sem surpresa</div>
        <h1 style={{ fontSize: 27, fontWeight: 700, color: 'var(--text-primary)', marginTop: 6, lineHeight: 1.1 }}>
          Veja os planos<br/>antes de começar
        </h1>
        <p style={{ fontSize: 14, color: 'var(--text-tertiary)', marginTop: 10, marginBottom: 22 }}>
          O nível free é de verdade — funcional e em português. Faça o upgrade quando quiser.
        </p>

        <Card variant="glow" rank="S" padding={18} style={{ marginBottom: 14 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 14 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Icon name="crown" size={20} color="var(--gold-400)" />
              <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 18, color: 'var(--text-primary)' }}>S-Rank</span>
            </div>
            <Badge tone="gold" variant="solid">7 dias grátis</Badge>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 9, marginBottom: 16 }}>
            <PlanRow>Quests ilimitadas e personalizadas por IA</PlanRow>
            <PlanRow>Master Quests semanais com +atributos</PlanRow>
            <PlanRow>Nutrição completa · macro tracking</PlanRow>
            <PlanRow>Card de perfil animado · sem anúncios</PlanRow>
          </div>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: 8, marginBottom: 14 }}>
            <span className="tnum" style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 26, color: 'var(--text-primary)' }}>R$ 14,90</span>
            <span style={{ fontSize: 13, color: 'var(--text-tertiary)' }}>/mês · ou R$ 99,90/ano</span>
          </div>
          <Button variant="gold" size="md" fullWidth onClick={onContinue}>Iniciar trial · sem cartão</Button>
        </Card>

        <Card padding={18} style={{ marginBottom: 18 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 14 }}>
            <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 17, color: 'var(--text-primary)' }}>Free Hunter</span>
            <span style={{ fontSize: 13, color: 'var(--text-tertiary)' }}>Grátis pra sempre</span>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 9, marginBottom: 16 }}>
            <PlanRow>1 quest diária · sistema de XP e rank completo</PlanRow>
            <PlanRow>Card de perfil compartilhável</PlanRow>
            <PlanRow on={false}>Quests ilimitadas e Master Quests</PlanRow>
          </div>
          <Button variant="secondary" size="md" fullWidth onClick={onContinue}>Continuar grátis</Button>
        </Card>
      </div>
    </div>
  );
}

/* ---------- Onboarding (respects the user 100%) ---------- */
const STEPS = [
  { key: 'goal', q: 'Qual é o seu objetivo?', multi: false, opts: ['Ganhar massa', 'Perder peso', 'Condicionamento', 'Força pura', 'Manter a forma'] },
  { key: 'level', q: 'Qual o seu nível agora?', multi: false, opts: ['Iniciante', 'Já treino às vezes', 'Treino há anos'] },
  { key: 'where', q: 'Onde você vai treinar?', multi: true, opts: ['Em casa', 'Academia', 'Ao ar livre', 'Sem equipamento'] },
  { key: 'days', q: 'Quantos dias por semana?', multi: false, opts: ['2 dias', '3 dias', '4 dias', '5+ dias'] },
];

function Onboarding({ onDone }) {
  const [step, setStep] = React.useState(0);
  const [answers, setAnswers] = React.useState({});
  const s = STEPS[step];
  const cur = answers[s.key] || (s.multi ? [] : null);
  const has = s.multi ? cur.length > 0 : !!cur;

  const pick = (opt) => {
    setAnswers((a) => {
      if (s.multi) {
        const arr = a[s.key] || [];
        return { ...a, [s.key]: arr.includes(opt) ? arr.filter((x) => x !== opt) : [...arr, opt] };
      }
      return { ...a, [s.key]: opt };
    });
  };
  const next = () => (step < STEPS.length - 1 ? setStep(step + 1) : onDone());

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: 'var(--grad-void)' }}>
      <StatusBar />
      <div style={{ display: 'flex', alignItems: 'center', gap: 14, padding: '4px 20px 18px' }}>
        <button onClick={() => (step ? setStep(step - 1) : null)} style={{ width: 36, height: 36, borderRadius: 'var(--radius-md)', background: 'var(--bg-surface)', border: '1px solid var(--border-default)', display: 'grid', placeItems: 'center', color: 'var(--text-secondary)', cursor: 'pointer', opacity: step ? 1 : 0.35 }}>
          <Icon name="chevronLeft" size={18} />
        </button>
        <div style={{ flex: 1, height: 6, borderRadius: 999, background: 'var(--ink-700)', overflow: 'hidden' }}>
          <div style={{ height: '100%', width: `${((step + 1) / STEPS.length) * 100}%`, background: 'var(--grad-energy)', borderRadius: 999, transition: 'width var(--dur-base) var(--ease-out)' }} />
        </div>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 12, color: 'var(--text-tertiary)' }}>{step + 1}/{STEPS.length}</span>
      </div>

      <div style={{ flex: 1, overflowY: 'auto', padding: '0 22px' }}>
        <h1 style={{ fontSize: 26, fontWeight: 700, color: 'var(--text-primary)', lineHeight: 1.15, marginBottom: 6 }}>{s.q}</h1>
        <p style={{ fontSize: 13.5, color: 'var(--text-tertiary)', marginBottom: 22 }}>
          {s.multi ? 'Selecione todas que se aplicam — vamos respeitar 100%.' : 'Escolha uma opção.'}
        </p>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 11 }}>
          {s.opts.map((opt) => {
            const sel = s.multi ? cur.includes(opt) : cur === opt;
            return (
              <button key={opt} onClick={() => pick(opt)} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                padding: '15px 18px', borderRadius: 'var(--radius-lg)', cursor: 'pointer', textAlign: 'left',
                fontFamily: 'var(--font-body)', fontSize: 15.5, fontWeight: 500,
                color: sel ? '#fff' : 'var(--text-secondary)',
                background: sel ? 'var(--grad-energy-soft), var(--bg-elevated)' : 'var(--bg-surface)',
                border: sel ? '1px solid color-mix(in srgb, var(--blue-400) 55%, transparent)' : '1px solid var(--border-default)',
                boxShadow: sel ? 'var(--glow-blue-sm)' : 'none', transition: 'all var(--dur-fast) var(--ease-out)' }}>
                {opt}
                {sel && <Icon name="check" size={18} color="var(--blue-300)" strokeWidth={3} />}
              </button>
            );
          })}
        </div>
      </div>

      <div style={{ padding: '14px 22px 36px' }}>
        <Button variant="primary" size="lg" glow={has} fullWidth disabled={!has} onClick={next}
          rightIcon={<Icon name="arrowRight" size={18} />}>
          {step < STEPS.length - 1 ? 'Continuar' : 'Forjar meu treino'}
        </Button>
      </div>
    </div>
  );
}

Object.assign(window, { Splash, Plans, Onboarding });
