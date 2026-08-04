const {Button,IconButton,Badge,Icon,OrderTicket,OrderTimer,StatusPill,SyncStatus,BrandMark,SegmentedControl,StatTile,EmptyState,AlertBanner}=window.NexoraDesignSystem_aa692a;

function Forno(){
  const ocupadas=FORNO.filter(Boolean).length;
  return <div style={{background:'var(--surface-card)',border:'1px solid var(--border-subtle)',borderRadius:'var(--brand-radius)',padding:14}}>
    <div style={{display:'flex',alignItems:'center',gap:8,marginBottom:12}}>
      <Icon name="local_fire_department" size={20} color="var(--nx-warning-500)"/>
      <span style={{font:'var(--fw-semibold) 15px/1 var(--font-sans)',color:'var(--text-primary)'}}>Forno</span>
      <span style={{marginLeft:'auto',font:'var(--type-numeric)',color:'var(--text-secondary)'}}>{ocupadas}/5</span></div>
    <div style={{display:'grid',gridTemplateColumns:'repeat(5,1fr)',gap:6}}>
      {FORNO.map((p,i)=><div key={i} style={{height:58,borderRadius:'var(--radius-md)',display:'flex',flexDirection:'column',alignItems:'center',justifyContent:'center',gap:2,
        background:p?'var(--nx-time-warn-bg)':'var(--surface-sunken)',border:'1px solid '+(p?'var(--nx-warning-500)':'var(--border-subtle)')}}>
        {p?<><span style={{font:'var(--fw-black) 18px/1 var(--font-mono)',color:'var(--nx-warning-500)'}}>{p.c}</span>
          <span style={{font:'400 11px/1 var(--font-mono)',color:'var(--text-secondary)'}}>{p.left}</span></>
          :<span style={{font:'var(--type-caption)',color:'var(--text-muted)'}}>livre</span>}</div>)}
    </div>
    {ocupadas<5?<div style={{marginTop:12}}><AlertBanner tone="danger" title="2 posições livres com fila esperando">Perda irrecuperável de capacidade — carregue o forno agora.</AlertBanner></div>:null}
  </div>;
}

function AllDay(){
  return <div style={{background:'var(--surface-card)',border:'1px solid var(--border-subtle)',borderRadius:'var(--brand-radius)',padding:14}}>
    <div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)',marginBottom:10}}>Contagem all-day</div>
    <div style={{display:'flex',flexDirection:'column',gap:8}}>
      {ALLDAY.map(([n,q])=><div key={n} style={{display:'flex',alignItems:'center',gap:10}}>
        <span style={{font:'var(--fw-black) 22px/1 var(--font-mono)',color:'var(--nx-cyan-400)',minWidth:28,textAlign:'right'}}>{q}</span>
        <span style={{font:'var(--fw-medium) 15px/1.2 var(--font-sans)',color:'var(--text-primary)'}}>{n}</span></div>)}
    </div></div>;
}

function KdsApp(){
  const [praca,setPraca]=React.useState('Todas');
  const [feitos,setFeitos]=React.useState([]);
  const [cmd,setCmd]=React.useState('');
  const fila=FILA.filter(p=>!feitos.includes(p.code));
  const concluir=c=>setFeitos(f=>[...f,c]);
  React.useEffect(()=>{const h=e=>{if(/^[0-9]$/.test(e.key))setCmd(c=>(c+e.key).slice(-2));
    if(e.key==='Enter'){setCmd(c=>{if(c)concluir(c);return '';});}
    if(e.key==='Backspace')setCmd(c=>c.slice(0,-1));};
    window.addEventListener('keydown',h);return()=>window.removeEventListener('keydown',h);},[]);
  const atrasados=fila.filter(p=>p.s>=600).length;
  return <div data-surface="kds" style={{height:'100vh',display:'flex',flexDirection:'column',background:'var(--surface-page)',color:'var(--text-primary)'}}>
    <header style={{flex:'0 0 auto',height:64,display:'flex',alignItems:'center',gap:20,padding:'0 20px',background:'var(--surface-card)',borderBottom:'1px solid var(--border-subtle)'}}>
      <BrandMark inverse size={22} subtitle="KDS · Praça de pizzas"/>
      <SegmentedControl options={['Todas','Montagem','Forno','Bebidas']} value={praca} onChange={setPraca}/>
      <div style={{marginLeft:'auto',display:'flex',alignItems:'center',gap:20}}>
        <div style={{textAlign:'right'}}><div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)'}}>Na fila</div>
          <div style={{font:'var(--fw-bold) 22px/1 var(--font-mono)'}}>{fila.length}</div></div>
        <div style={{textAlign:'right'}}><div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)'}}>Atrasados</div>
          <div style={{font:'var(--fw-bold) 22px/1 var(--font-mono)',color:atrasados?'var(--nx-time-late)':'var(--text-primary)'}}>{atrasados}</div></div>
        <div style={{textAlign:'right'}}><div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)'}}>Média 1h</div>
          <div style={{font:'var(--fw-bold) 22px/1 var(--font-mono)'}}>11:40</div></div>
        <SyncStatus state="local" queued={62}/>
      </div>
    </header>
    <div style={{flex:'1 1 auto',display:'flex',minHeight:0}}>
      <div style={{flex:'1 1 auto',overflowY:'auto',padding:20}}>
        {fila.length?<div style={{display:'grid',gridTemplateColumns:'repeat(auto-fill,minmax(316px,1fr))',gap:16,alignItems:'start'}}>
          {fila.map(p=><OrderTicket key={p.code} code={p.code} where={p.where} channel={p.ch} seconds={p.s} items={p.itens} fireAt={p.fire}
            footer={<Button variant="accent" size="lg" iconLeft="check" onClick={()=>concluir(p.code)}>Pronto · {p.code}</Button>}/>)}
        </div>:<EmptyState icon="restaurant" title="Fila vazia" action={<Button variant="secondary" onClick={()=>setFeitos([])}>Recarregar turno</Button>}>
          Nenhum item aguardando produção nesta praça.</EmptyState>}
      </div>
      <aside style={{flex:'0 0 300px',borderLeft:'1px solid var(--border-subtle)',padding:20,display:'flex',flexDirection:'column',gap:16,overflowY:'auto'}}>
        <Forno/><AllDay/>
        <div style={{background:'var(--surface-card)',border:'1px solid var(--border-subtle)',borderRadius:'var(--brand-radius)',padding:14}}>
          <div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)',marginBottom:10}}>Comando</div>
          <div style={{height:58,borderRadius:'var(--radius-md)',background:'var(--surface-sunken)',border:'1px solid var(--border-default)',display:'flex',alignItems:'center',justifyContent:'center',
            font:'var(--fw-black) 32px/1 var(--font-mono)',color:cmd?'var(--nx-cyan-400)':'var(--text-muted)',letterSpacing:'.1em'}}>{cmd||'––'}</div>
          <div style={{font:'var(--type-caption)',color:'var(--text-muted)',marginTop:8,lineHeight:1.5}}>Digite o número do pedido e <strong style={{color:'var(--text-secondary)'}}>Enter</strong> para concluir. Backspace apaga. Sem mouse, sem digitação livre.</div>
          <div style={{display:'flex',gap:8,marginTop:12}}>
            <Button variant="secondary" size="sm" iconLeft="block">Falta insumo</Button>
            <Button variant="secondary" size="sm" iconLeft="refresh">Refazer</Button></div>
        </div>
      </aside>
    </div>
  </div>;
}
window.KdsApp=KdsApp;
